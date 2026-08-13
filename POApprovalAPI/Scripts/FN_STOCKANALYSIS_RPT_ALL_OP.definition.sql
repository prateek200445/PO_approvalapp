
-- select * from [FN_STOCKANALYSIS_RPT_ALL_OP] ('HCP Plastene Bulkpack Ltd','2023-07-01',0,0)


CREATE FUNCTION [dbo].[FN_STOCKANALYSIS_RPT_ALL_OP]
(
	@TempCompan VARCHAR(MAX),
	@DATEFROM DATE,
	 
	@RptType int =0,
	@intOp int =0
) 
RETURNS @t table(CompanyName varchar(150),sysdate date,Deptt varchar(40),Groupname varchar(40),SubGroupname varchar(40), 
ItemName varchar(500) ,BalanceQty float,[Type] VARCHAR(50),itemcode varchar(50))
AS
BEGIN
 	     declare @companyname table (StringValue varchar(100))	 
		INSERT INTO @companyname SELECT * FROM Split(@TempCompan,',')


		declare @qtyPar int  =3--(select value from Loginentry.dbo.erp_setting where name='qty') 
	    declare @ParaDATEFROM DATE =@DATEFROM		 
		declare @ParaRptType int =@RptType
		declare @Paraopdate DATE  = dateadd(DD,-1, @ParaDATEFROM)
		declare @openingdate date 

		set @openingdate = (select distinct OpeningBalanceDate  from OpeningBalanceCommoditywise where companyName = @TempCompan)

		set @openingdate = DATEADD(dd,1,@openingdate)
		 
		declare @temp table (companyname varchar(150),sysdate date,Qty float ,MainGroup varchar(50),GroupName varchar(50),
		SubGroupName varchar(50),Type varchar(50),ItemName varchar(500),OrderType int ,itemcode varchar(50) ) --,OrderType int ,Sort int		  


		 

		insert into @temp
		--production data fromdate
		SELECT  companyname,sysdate,SUM(qty) QTY,Deptt,GroupName,
		'Total Production Own+JW' ,SubGroupName,ITEMNAME ,8 as OtherT,ITEMCODE
		FROM [VW_PRODUCTION_STK_FG] with(nolock) 
		inner join @companyname t on [VW_PRODUCTION_STK_FG].companyname = t.StringValue
		--where  sysdate >=@PARADATEFROM    
        where  (sysdate between @openingdate and @Paraopdate)  and sysdate >= @openingdate    
		GROUP BY companyname,sysdate,Deptt,GroupName,SubGroupName,ITEMNAME,ITEMCODE
		-- end production data
			
			union all
		
		--- change by manish on 28th April 2025 adding condition of itemmaster
		select SJ.companyname,sysdate,qty,I.deptt,I.groupname,type,I.SubGroupName,I.ItemName,srno,SJ.itemcode 
		from stockjv SJ
		inner join Item I on I.ItemCode = SJ.itemcode and  I.CompanyName = SJ.CompanyName 
		inner join @companyname t on t.StringValue = SJ.companyname  
		 where  (sysdate between @openingdate and @Paraopdate)  and sysdate >= @openingdate    
		--- end comment
		UNION ALL
		SELECT  companyname,sysdate,SUM(qty) QTY,Deptt,GroupName,'Total Consumption Own+JW',SubGroupName,
		VW_Consumption_STK_FG.itemname ,9 as OtherT,ITEMCODE
		FROM VW_Consumption_STK_FG with(nolock) 
		inner join @companyname t on VW_Consumption_STK_FG.companyname = t.StringValue
	--	where  sysdate >=@PARADATEFROM  --and Deptt in ('RM','SF','FG','RM Consumables') 
	    where  (sysdate between @openingdate and @Paraopdate)  and sysdate >= @openingdate    
		GROUP BY companyname,sysdate,Deptt,GroupName,SubGroupName,VW_Consumption_STK_FG.itemname,ITEMCODE

		--insert into @temp
		;with temp_CTE as  
		(
		select 
		companyname,
		CAST(sysdate AS DATE) sysdate,MainGroup,A.GroupName,
		case when isnull(SubGroupName,'')='' then A.GroupName else isnull(SubGroupName,'') end SubGroupName, (Qty) AS Qty ,Type ,
		ItemName ,		ItemCode 
		--OrderT,case when MainGroup='RM' then 1 when MainGroup='SF' then 2 when MainGroup='FG' then 3 else 4 end as Sort,
		from (
		select companyname,sysdate,sum(Qty) as Qty,MainGroup,GroupName,Type,SubGroupName,ItemName,OrderT,ItemCode		
		from 
		(
			select 	companyname,sysdate,MRNo,SRNO,
				case when unit != 'KGS' then isnull((netwt),0) else isnull((acceptedqty),0) end as qty,

			--isnull((acceptedqty),0) as qty,
			Vw_StoreInwards.itemDeptt as MainGroup,Vw_StoreInwards.GroupName , 
			case when  FirmGSTIn= VendorGST then 'Branch Tfr Recd' 
			when Categoryseries ='JBIN-SE' then 'Recd from JW (Own)' 	when Categoryseries ='JBIN-OT' then 'Recd For JW (Others)' 
			else   'Purchase'   end as Type ,
			SubGroupName ,ItemName ,
			case when  FirmGSTIn= VendorGST then 1 when Categoryseries ='JBIN-SE' then 3 	
			when Categoryseries ='JBIN-OT' then 4	else   2   end as OrderT ,ItemCode
			from Vw_StoreInwards  with(nolock)  
			inner join @companyname t on CompanyName = t.StringValue
			where     Category not in ('JOB IN') and   Vw_StoreInwards.Cancel !='Cancelled'
			and itemDeptt in ('RM','SF','FG','RM Consumables')
--			and SysDate >=@PARADATEFROM
	        and  (sysdate between @openingdate and @Paraopdate)  and sysdate >= @openingdate    
	
		) a  group by  CompanyName,SysDate, MainGroup,GroupName,Type,SubGroupName,ItemName,OrderT,ItemCode

		union all

		select CompanyName ,SysDate,
		case when unit != 'KGS' then isnull(sum(netwt),0) else isnull(sum(acceptedqty),0) end as qty,
		--, isnull(sum(acceptedqty),0) qty,
		Vw_StoreInwards.itemDeptt as MainGroup,Vw_StoreInwards.GroupName , 
		case when  FirmGSTIn= VendorGST then 'Branch Tfr Recd'  	
			when Categoryseries ='JBIN-SE' then 'Recd from JW (Own)' 	
			when Categoryseries ='JBIN-OT' then 'Recd For JW (Others)' 
			else   'Purchase'   end as Type ,SubGroupName,ItemName ,

			case when  FirmGSTIn= VendorGST then 1	
			when Categoryseries ='JBIN-SE' then 2	
			when Categoryseries ='JBIN-OT' then 4 	
			else   2   end as OrderType,ItemCode


		from Vw_StoreInwards  with(nolock) inner join @companyname t on CompanyName = t.StringValue 
		where   
		Category   in ('JOB IN') and Cancel !='Cancelled'
		 and itemDeptt in ('RM','SF','FG','RM Consumables')
		  and  (sysdate between @openingdate and @Paraopdate)  and sysdate >= @openingdate    
	   --and Categoryseries !='JBIN-OT'
		
		 --and SysDate >=@PARADATEFROM
		group by SysDate,CompanyName,GroupName ,itemDeptt ,VendorGST,Categoryseries,FirmGSTIn,SubGroupName,ItemName,ItemCode,Unit


		--union all

		--select  	v.ProcessorName as CompanyName,v.MainChallanDate
		--,sum(V.OrderQty) ,I.Deptt,i.GroupName,'Recd For JW (Others)' Type,
		--i.SubGroupName ,i.ItemName , 4 OrderType,v.ItemCode
		--from   Despatch.DBO.vw_subsidiaryChallanItem v with(nolock)  		
		--inner join MaterialProcessing.DBO.item i on V.Itemcode=i.ItemCode and i.CompanyName=v.ProcessorName 
		--where  CAST( V.MainChallanDate AS DATE) between @openingdate and @Paraopdate
		--and ISNULL(v.isfreeze,0) = 0 and  i.Deptt  in ('RM','SF','FG','RM Consumables')
		--group by v.ProcessorName ,v.MainChallanDate
		-- ,I.Deptt,i.GroupName,		i.SubGroupName ,i.ItemName ,v.ItemCode

		---- debit note added on 27th Sep 2022
		 union all
		select  v.CompanyName,sysdate,-SUM(QtyDifference) AS QTY,W.Deptt,W.GroupName,'Purchase' , W.SubGroupName,W.ItemName,
		13 as OtherT,V.ItemCode
		from vw_DebitNote V  with(nolock)  inner join
		Item W   with(nolock)  on W.ItemCode=V.ItemCode and w.CompanyName=v.CompanyName 
		--inner join item with(nolock) on item.ItemCode=v.ItemCode and item.CompanyName=v.CompanyName
		inner join @companyname t on W.CompanyName = t.StringValue
		where CAST( Sysdate AS DATE) between  @openingdate and @Paraopdate 
		and Sysdate >= @openingdate
		and 
		W.Deptt in ('RM','SF','FG','RM Consumables') and DebitType = 'Qty Difference'
		 GROUP BY  v.CompanyName,sysdate,W.Deptt,W.GroupName, W.SubGroupName,W.ItemName,V.ItemCode,DebitNoteNumber
	  --- end

		union all
		select v.CompanyName ,InwardDate,sum(qty),i.Deptt as  MaingroupName,i.GroupName,'Warehouse Inwards' Type,
		I.SubGroupName,V.ItemName,2 OrderType,w.ItemCode
		from WareHouseInwards V with(nolock) inner join
		warehouse W with(nolock)  on W.ItemCode=V.ItemCode and w.CompanyName=v.CompanyName and w.WareHouseName=v.ToWareHouse
		inner join item i with(nolock)  on W.ItemCode=i.ItemCode and w.CompanyName=i.CompanyName   and 
		i.ItemCode=V.ItemCode and i.CompanyName=v.CompanyName  
		inner join @companyname t on v.CompanyName = t.StringValue
		where v.transid =0  and 
		i.Deptt in ('RM','SF','FG','RM Consumables')
	     and  (InwardDate between @openingdate and @Paraopdate)  and InwardDate >= @openingdate    
		--and InwardDate >=@PARADATEFROM
		GROUP BY i.GroupName,v.InwardDate, v.CompanyName,i.Deptt,I.SubGroupName,V.ItemName,w.ItemCode

		union all

		select PurchaseVoucherItem.CompanyName ,PurchaseVoucher.SysDate,sum(ActualQty) as ActualQty,
		item.Deptt as  MainGroupName,item.GroupName , 
		case when F.NewGSTNo=L.NewGSTNo then  'Branch Tfr Recd'  
		when PurchaseVoucher.VoucherType='Job Invoice' 
		then 'Recd from JW (Own)' else 'Purchase' end ,item.SubGroupName ,ITEM.ItemName ,
		case when F.NewGSTNo=L.NewGSTNo then  1 	when PurchaseVoucher.VoucherType='Job Invoice' then 3 else 2 end as OtherT,
		PurchaseVoucherItem.ItemCode
		from PurchaseVoucherItem with(nolock)  
		inner join item with(nolock)  on item.CompanyName=PurchaseVoucherItem.CompanyName and 		item.ItemCode=PurchaseVoucherItem.ItemCode 
		inner join PurchaseVoucher with(nolock)  on 
		PurchaseVoucher.StoreInwardNo=PurchaseVoucherItem.StoreInwardNo and 
		PurchaseVoucherItem.CompanyName=PurchaseVoucher.CompanyName 
		inner join ledgermaster L with(nolock) On L.CompanyName=PurchaseVoucherItem.CompanyName and 
		l.LedgerName=PurchaseVoucher.SupplierName
		inner join FactoryInfo f with(nolock)  on F.Name=PurchaseVoucher.CompanyName and f.SrNo=PurchaseVoucher.companyId
		inner join @companyname t on PurchaseVoucher.CompanyName = t.StringValue
		where (PurchaseVoucher.SysDate between @openingdate and @Paraopdate)  and   PurchaseVoucher.SysDate >= @openingdate    
		
		--  PurchaseVoucher.SysDate >=@PARADATEFROM 
		and item.Deptt in ('RM','SF','FG','RM Consumables') and PurchaseVoucher.StoreInwardNo in (
		select distinct p.StoreInwardNo from PurchaseVoucherItem p with(nolock)  inner join PurchaseVoucher V with(nolock)  
		on p.StoreInwardNo=v.StoreInwardNo and 
		p.CompanyName=v.CompanyName 
		except
		select 	distinct SrNo
		from Vw_StoreInwards  with(nolock)  where    Vw_StoreInwards.Cancel !='Cancelled' --and Category not in ('JOB IN')
		) group by PurchaseVoucherItem.CompanyName ,PurchaseVoucher.SysDate,item.Deptt,item.GroupName,l.NewGSTNo,VoucherType,
		F.NewGSTNo,item.SubGroupName,ITEM.ItemName,PurchaseVoucherItem.ItemCode

		union all
		select I.companyname,S.InvDate AS DespatchDate, SUM(ActualQty) AS qTY ,C.Deptt,C.GroupName, 
		case when F.NewGSTNo=L.NewGSTNo then  'Br Transfer Sent' else 'Sales' end Type,C.SubGroupName,C.ItemName ,
		case when F.NewGSTNo=L.NewGSTNo then  6 else 5 end  as OtherT,I.ITEMCODE

		from 
		SalesVoucher S with(nolock) inner join SalesVoucherItem I with(nolock) on 
		S.companyId=I.companyId and S.CompanyName=I.CompanyName and S.InvNo=I.InvNo and s.InvDate=i.InvDate and s.InvYear=i.Invyear
		inner join LedgerMaster L with(nolock) On L.CompanyName=S.CompanyName and L.LedgerName=S.BuyerName
		left join Item C with(nolock) on C.CompanyName=I.CompanyName and C.itemcode=I.itemcode
		inner join FactoryInfo F with(nolock) on f.name=s.CompanyName and f.SrNo=S.companyId 
		inner join @companyname t on s.CompanyName = t.StringValue
		WHERE (S.InvDate between @openingdate and @Paraopdate)  and   S.InvDate >= @openingdate    
	--	 S.InvDate >=@PARADATEFROM 
		and  S.VoucherType <>'Job Invoice'  --13.10.2021 
		and C.Deptt in ('RM','SF','FG','RM Consumables') 
		GROUP BY  I.companyname,s.InvDate,I.Commodity,s.InvNo,C.Deptt,C.GroupName,l.NewGSTNo,S.VoucherType,F.NewGSTNo,
		C.SubGroupName,C.ItemName,i.ITEMCODE

		union all
		--FORMAT SYSDATE TO ONLY DATE 
		select vw_challan5a.companyname ,CAST(sysdate AS DATE) AS SYSDATE,sum(ItemQty)'Qty',vw_challan5a.Deptt,vw_challan5a.GroupName,
		case when vw_challan5a.NewGSTNo=[factoryGSTNo] then 'Br Transfer Sent' else 'Sent for JW (Own)' end Type ,
		vw_challan5a.SubGroupName,vw_challan5a.ITEMNAME ,
		case when vw_challan5a.NewGSTNo=[factoryGSTNo] then 6 else 7 end  as OtherT,vw_challan5a.ItemCode
		from Despatch.dbo.vw_challan5a  with(nolock)		
		inner join @companyname t on vw_challan5a.companyname = t.StringValue
		where --CAST(sysdate AS DATE) >=@PARADATEFROM 
		 (CAST(sysdate AS DATE) between @openingdate and @Paraopdate)  and   CAST(sysdate AS DATE) >= @openingdate 
		and (iscancel is null or iscancel = '')  
		and vw_challan5a.Deptt in ('RM','SF','FG','RM Consumables') 
		group by vw_challan5a.companyname ,sysdate,  vw_challan5a.Deptt,vw_challan5a.GroupName,vw_challan5a.NewGSTNo,
		[factoryGSTNo],vw_challan5a.SubGroupName,vw_challan5a.ITEMNAME,vw_challan5a.ItemCode

		union all
		select f.ProcessorName, f.Date,sum(Qty) qty , f.Deptt,f.GroupName,'Return JW(Others)',
		f.SubGroupName,f.ItemName ,8 as OtherT,f.ItemCode
		from Despatch.dbo.vw_SubChallanListMulti f with(nolock) 
		inner join @companyname t on f.ProcessorName = t.StringValue
		--inner join Despatch.dbo.[vw_subsidiaryChallanItem] VS on VS.MainChallanNo = f.mainchallanno
		--and VS.MainChallanDate = f.MainChallanDate
		--and VS.SubsidiaryBuyer = f.SubsidiaryBuyer
		--and f.commodityname = VS.commodityname
		 where  --f.Date >=@PARADATEFROM  
		 (f.Date between @openingdate and @Paraopdate)  and   f.Date >= @openingdate 
		 and ( isnull(f.iscancel,'no')='no' or  isnull(f.iscancel,'')='')
		 and f.Deptt in ('RM','SF','FG','RM Consumables') 
		group by f.Date, f.GroupName, f.ProcessorName,f.Deptt,f.GroupName,f.SubGroupName,f.ItemName,f.ItemCode

		 

		UNION ALL

		Select  
		a.companyname,A.sysdate,SUM(A.AcceptedQty) QTY,I.Deptt,I.GroupName ,'Production at JW',i.SubGroupName,
		i.ItemName ,10 as OtherT,a.ItemCode
		from Challan5AInward a with(nolock)  inner join item i with(nolock)  on i.ItemCode=a.Itemcode and i.CompanyName=a.companyname
		inner join @companyname t on a.companyname = t.StringValue
		 where 
--		 a.sysdate >=@PARADATEFROM 
		 	 (a.sysdate between @openingdate and @Paraopdate)  and   a.sysdate >= @openingdate 
		  and I.Deptt in ('RM','SF','FG','RM Consumables') 
		--where Deptt not in ('RM','RM Consumables') -- added 13.10.2021
		GROUP BY a.companyname,A.sysdate,I.Deptt,I.GroupName,i.SubGroupName,i.ItemName,a.ItemCode

		UNION ALL
		--13.11.2021
		 Select Challan5AInward.companyname,Challan5AInward.sysdate,SUM(Challan5AInward.AcceptedQty) QTY,
		ITEM.Deptt,ITEM.GroupName ,'Consumption At JW',
		item.SubGroupName,item.ItemName ,11 as OtherT,Challan5AInward.Itemcode
		from Challan5AInward with(nolock)  INNER JOIN Despatch..Vw_Challan5A ChallanItem with(nolock)  on 
		Challan5AInward.ChallanSubCode=ChallanItem.ChallanNo + '/' + cast(ChallanItem.SubCode as varchar(2))						
		INNER JOIN ITEM with(nolock)  ON ITEM.ItemCode=ChallanItem.ItemCode AND ITEM.CompanyName=Challan5AInward.companyname
		inner join @companyname t on Challan5AInward.companyname = t.StringValue
		 where
	--	  Challan5AInward.sysdate >=@PARADATEFROM  
		  ( Challan5AInward.sysdate between @openingdate and @Paraopdate)  and    Challan5AInward.sysdate >= @openingdate
		 and  ITEM.Deptt in ('RM','SF','FG','RM Consumables') 
		GROUP BY Challan5AInward.companyname,Challan5AInward.sysdate,ITEM.Deptt,ITEM.GroupName, item.SubGroupName,item.ItemName
		,Challan5AInward.Itemcode

 
		--end 13.11.2021
		UNION ALL

		select  V.ProcessorName ,V.DATE,SUM(v.Qty) AS QTY,  v.Deptt,v.GroupName,'Pord. of JW',v.SubGroupName,v.ItemName 
		,12 as OtherT,v.ItemCode
		from Despatch.DBO.vw_SubChallanListMulti v with(nolock) 
		--inner join MaterialProcessing.dbo.item  i with(nolock)  on i.itemcode=v.Itemcode and i.CompanyName=v.ProcessorName
		inner join Despatch.DBO.vw_subsidiaryChallanItem O with(nolock) on O.ProcessorName=V.ProcessorName and 
		O.MainChallanDate=v.MainChallanDate and O.MainChallanNo=v.MainChallanNo
		--	and v.commodityname = o.commodityname
	
		inner join MaterialProcessing.dbo.item i1 with(nolock)  on i1.itemcode =o.itemcode and i1.CompanyName=O.ProcessorName
		inner join @companyname t on V.ProcessorName= t.StringValue
		--BY raj 18.11.2021 as discuss not shown same return itemcode
		where
--		 V.DATE >=@PARADATEFROM 
		  ( V.DATE between @openingdate and @Paraopdate)  and    V.DATE >= @openingdate
		 and  O.Itemcode!=v.Itemcode  
		and  v.Deptt in ('RM','SF','FG','RM Consumables') 
		--end by raj
		GROUP BY V.ProcessorName ,V.DATE,v.Deptt,v.GroupName,v.SubGroupName,v.ItemName,v.ItemCode

		UNION ALL

		select  V.ProcessorName ,V.DATE,SUM(v.Qty) AS QTY,  o.Deptt,o.GroupName,'Consumption of JW',o.SubGroupName,o.ItemName , 
		13 as OtherT,o.ItemCode
		from Despatch.DBO.vw_SubChallanListMulti v with(nolock) 
		--inner join MaterialProcessing.dbo.item  i with(nolock)  on i.itemcode=v.Itemcode and i.CompanyName=v.ProcessorName
		inner join Despatch.DBO.vw_subsidiaryChallanItem O with(nolock)  on O.ProcessorName=V.ProcessorName and 
		O.MainChallanDate=v.MainChallanDate and O.MainChallanNo=v.MainChallanNo
		and v.commodityname = o.commodityname
		--inner join MaterialProcessing.dbo.item i1 with(nolock)  on i1.itemcode =o.itemcode and i1.CompanyName=O.ProcessorName
		inner join @companyname t on V.ProcessorName= t.StringValue
		--BY raj 18.11.2021 as discuss not shown same return itemcode
		left join Despatch.DBO.vw_subsidiaryChallanItem L with(nolock)  on L.ProcessorName=V.ProcessorName and 
		L.MainChallanDate=v.MainChallanDate and L.MainChallanNo=v.MainChallanNo
		where 
		--  V.DATE >=@PARADATEFROM
			  ( V.DATE between @openingdate and @Paraopdate)  and    V.DATE >= @openingdate
		 and L.Itemcode!=v.Itemcode 
		and  o.Deptt  in ('RM','SF','FG','RM Consumables') 
		--end 18.11.2021
		GROUP BY  V.ProcessorName ,V.DATE,o.Deptt,o.GroupName,o.SubGroupName,o.ItemName,o.ItemCode

		--added Stock Adju entry 
		union All

		select p.vCompanyName,p.dSysdate, sum(fPendingQty) qty ,i.Deptt,i.GroupName,'Stock Adjustment Entry' ,i.SubGroupName,i.ItemName,
		13 as OtherT,p.FGITEMCODE
		from Prod_RMD_InOut p with(nolock) inner join item i with(nolock) on 
		I.ItemCode=p.FGITEMCODE and i.CompanyName=p.vCompanyName
		where
--		  p.dSysdate >=@PARADATEFROM  
			  (p.dSysdate between @openingdate and @Paraopdate)  and      p.dSysdate >= @openingdate
		and vToGodown='Stock Adjustment Entry'  and  i.Deptt  in ('RM','SF','FG','RM Consumables') 
		group by  p.vCompanyName,p.dSysdate,i.Deptt,i.GroupName,i.SubGroupName,i.ItemName,p.FGITEMCODE 
				 
		union all

		 
		select  v.CompanyName,sysdate,SUM(qty) AS QTY,w.Deptt,w.GroupName,'Stock Adjustment Entry' , w.SubGroupName,w.ItemName,
		13 as OtherT,V.ItemCode
		from WarehousetoWareHouse V  with(nolock)  inner join
		WareHouse W   with(nolock)  on W.ItemCode=V.ItemCode and w.CompanyName=v.CompanyName and w.WareHouseName=v.ToWareHouse
		--inner join item with(nolock) on item.ItemCode=v.ItemCode and item.CompanyName=v.CompanyName
		where --sysdate >=@PARADATEFROM  
		(sysdate between @openingdate and @Paraopdate)  and      sysdate >= @openingdate
		and ToWareHouse = 'Stock Adjustment Entry' 
		  and  w.Deptt  in ('RM','SF','FG','RM Consumables')
		 GROUP BY  v.CompanyName,sysdate,w.Deptt,w.GroupName, w.SubGroupName,w.ItemName,V.ItemCode

		 union all
		 select * from @temp
		) A 
		--inner join FactoryInfo F with(nolock) on F.Name=A.CompanyName change on 3.12.2021
		inner join @companyname t on A.CompanyName = t.StringValue
		--where   A.SysDate >=@PARADATEFROM  
	
		 union all
			-- --Opending calculation base on STK In Hand 
			 select o.companyname,o.OpeningBalanceDate,Deptt,isnull(GroupName,''),isnull(SubGroupName,''), o.OpeningBalance,'Op.Factory Owned',ItemName,o.ItemCode
			 from OpeningBalanceCommoditywise o with(nolock) inner join item on item.ItemCode 
			 = o.ItemCode and item.CompanyName = o.companyName
			 inner join @companyname C on C.StringValue=item.CompanyName
			where  OpeningBalanceDate <= @Paraopdate-- and o.companyName= @TempCompan
	
		 --union all
			-- --Opending calculation base on STK In Hand 
			-- select companyname,DATEADD(D,-1,@PARADATEFROM),Deptt,isnull(GroupName,''),isnull(SubGroupName,''), stkinhand,'Op.Factory Owned',ItemName,ItemCode
			-- from item with(nolock) inner join @companyname C on C.StringValue=item.CompanyName
			-- WHERE  Deptt in ('RM','SF','FG','RM Consumables') and round(stkinhand,2)!=0

			-- union all
		 
			-- select WareHouse.companyname,DATEADD(D,-1,@PARADATEFROM),WareHouse.Deptt,isnull(WareHouse.GroupName,''),isnull(Item.SubGroupName,''), 
			-- WareHouse.stkinhand,'Op.Factory Owned',WareHouse.ItemName,WareHouse.ItemCode
			-- from WareHouse with(nolock) inner join @companyname C on C.StringValue=WareHouse.CompanyName
			-- inner join item WITH(NOLOCK) on item.CompanyName=WareHouse.CompanyName and item.ItemCode=WareHouse.ItemCode
			-- where warehousename not in ('Despatch Godown','Stock Adjustment Entry')  and round(WareHouse.stkinhand,2)!=0
		 )  
		 insert into @t 
		 SELECT *
		  FROM (
		 --Op.Factory Owned 
		  --Op.Factory Owned 
		 select  companyname,@PARADATEFROM AS sysdate,MainGroup,isnull(GroupName,'') GroupName,
		 isnull(SubGroupName,'') SubGroupName,isnull(ItemName,'') ItemName,	 
		   round(
		 ISNULL(sum([Op.Factory Owned]),0) 
		 +
		 (ISNULL(sum([Branch Tfr Recd]),0) +  ISNULL(sum([Purchase]),0) + ISNULL(SUM([Warehouse Inwards]),0) + ISNULL(sum([Recd from JW (Own)]),0)   )
		 -
		 ( 
				ISNULL(sum([Sales]),0) +		 
				ISNULL(sum([Br Transfer Sent]),0) +	 
				ISNULL(sum([Sent for JW (Own)]),0) +
				(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0)) +		 
				ISNULL(sum([Stock Adjustment Entry]),0)
		 ) 
		 
		 +

		 ((ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0))) ,@qtyPar)  		 as [Cl.Factory Owned],

		 'Op.Factory Owned' as [Type],A.ITEMCODE
		 from temp_CTE
		    
		pivot
		(

		sum(Qty) for Type in ([Op.Factory Owned],[Purchase],[Warehouse Inwards],[Branch Tfr Recd],[Recd from JW (Own)], [Sales],
		[Br Transfer Sent],[Sent for JW (Own)], [Total Production Own+JW],[Total Consumption Own+JW],
		 [Pord. of JW],[Consumption Of JW],[Stock Adjustment Entry])
		) A 
		where a.MainGroup in ('RM','FG','SF','RM Consumables' )
		group by  A.CompanyName,  a.MainGroup,GroupName,a.SubGroupName,A.ItemName,a.itemcode 

		) a WHERE isnull(a.[Cl.Factory Owned],0)!=0 
 
	RETURN
END






