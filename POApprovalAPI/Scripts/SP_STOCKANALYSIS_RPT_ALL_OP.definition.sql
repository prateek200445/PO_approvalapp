
 

CREATE PROCEDURE [dbo].[SP_STOCKANALYSIS_RPT_ALL_OP]
(
	@companyname StringArray READONLY, 
	@DATEFROM DATE,
	@DATEto DATE,
	@RptType int =0,
	@intOp int =0
) WITH RECOMPILE
 as
BEGIN
		CHECKPOINT
		DBCC DROPCLEANBUFFERS 
		DBCC FREEPROCCACHE		 


		declare @qtyPar int  =3--(select value from Loginentry.dbo.erp_setting where name='qty') 
	    declare @ParaDATEFROM DATE =@DATEFROM
		declare @ParaDATEto DATE =@DATEto
		declare @ParaRptType int =@RptType
		declare @Paraopdate DATE  = dateadd(DD,-1, @ParaDATEFROM)

		 
		declare @temp table (companyname varchar(150),sysdate date,MainGroup varchar(50),GroupName varchar(50),
		SubGroupName varchar(50),Qty float ,Type varchar(50),ItemName varchar(500) ,itemcode varchar(50) ) --,OrderType int ,Sort int		  

		insert into @temp
		select 
		companyname,
		CAST(sysdate AS DATE),MainGroup,A.GroupName,
		case when isnull(SubGroupName,'')='' then A.GroupName else isnull(SubGroupName,'') end SubGroupName, (Qty) AS Qty ,Type ,
		ItemName ,		ItemCode 
		--OrderT,case when MainGroup='RM' then 1 when MainGroup='SF' then 2 when MainGroup='FG' then 3 else 4 end as Sort,
		from (
		select companyname,sysdate,sum(Qty) as Qty,MainGroup,GroupName,Type,SubGroupName,ItemName,OrderT,ItemCode		
		from 
		(
			select 	companyname,sysdate,MRNo,SRNO,
			isnull((acceptedqty),0) as qty,
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
		) a  group by  CompanyName,SysDate, MainGroup,GroupName,Type,SubGroupName,ItemName,OrderT,ItemCode

		union all

		select CompanyName ,SysDate, isnull(sum(acceptedqty),0) qty,
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
		group by SysDate,CompanyName,GroupName ,itemDeptt ,VendorGST,Categoryseries,FirmGSTIn,SubGroupName,ItemName,ItemCode

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
		where item.Deptt in ('RM','SF','FG','RM Consumables') and PurchaseVoucher.StoreInwardNo in (
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
		WHERE S.VoucherType <>'Job Invoice'  --13.10.2021 
		and C.Deptt in ('RM','SF','FG','RM Consumables') 
		GROUP BY  I.companyname,s.InvDate,I.Commodity,s.InvNo,C.Deptt,C.GroupName,l.NewGSTNo,S.VoucherType,F.NewGSTNo,
		C.SubGroupName,C.ItemName,i.ITEMCODE

		union all
		--FORMAT SYSDATE TO ONLY DATE 
		select vw_challan5a.companyname ,CAST(sysdate AS DATE) AS SYSDATE,sum(ItemQty)'Qty',item.Deptt,item.GroupName,
		case when vw_challan5a.NewGSTNo=[factoryGSTNo] then 'Br Transfer Sent' else 'Sent for JW (Own)' end Type ,
		item.SubGroupName,ITEM.ITEMNAME ,
		case when vw_challan5a.NewGSTNo=[factoryGSTNo] then 6 else 7 end  as OtherT,vw_challan5a.ItemCode
		from Despatch.dbo.vw_challan5a  with(nolock) inner join Item   with(nolock) on  
		item.ItemCode= vw_challan5a.ItemCode and 
		Item.companyname = vw_challan5a.companyname   
		inner join @companyname t on vw_challan5a.companyname = t.StringValue
		where  (iscancel is null or iscancel = '')  
		and Item.Deptt in ('RM','SF','FG','RM Consumables') 
		group by vw_challan5a.companyname ,sysdate,  item.Deptt,item.GroupName,vw_challan5a.NewGSTNo,
		[factoryGSTNo],item.SubGroupName,ITEM.ITEMNAME,vw_challan5a.ItemCode

		union all
		select f.ProcessorName, f.Date,sum(Qty) qty , i.Deptt,i.GroupName,'Return JW(Others)',
		i.SubGroupName,I.ItemName ,8 as OtherT,f.ItemCode
		from Despatch.dbo.vw_SubChallanListMulti f with(nolock) 
		inner join MaterialProcessing.dbo.item i with(nolock) 
		on i.companyname=f.ProcessorName and i.ItemCode=f.Itemcode
		inner join @companyname t on f.ProcessorName = t.StringValue
		 where ( isnull(f.iscancel,'no')='no' or  isnull(f.iscancel,'')='')
		 and i.Deptt in ('RM','SF','FG','RM Consumables') 
		group by f.Date, i.GroupName, f.ProcessorName,i.Deptt,i.GroupName,i.SubGroupName,I.ItemName,f.ItemCode

		UNION ALL

		SELECT  companyname,sysdate,SUM(qty) QTY,Deptt,GroupName,'Total Production Own+JW' ,
		SubGroupName,ITEMNAME ,8 as OtherT,ITEMCODE
		FROM [VW_PRODUCTION_STK_FG] with(nolock) 
		inner join @companyname t on [VW_PRODUCTION_STK_FG].companyname = t.StringValue
		 where  Deptt in ('RM','SF','FG','RM Consumables') 
		GROUP BY companyname,sysdate,Deptt,GroupName,SubGroupName,ITEMNAME,ITEMCODE

		UNION ALL
		SELECT  companyname,sysdate,SUM(qty) QTY,Deptt,GroupName,
		'Total Consumption Own+JW',SubGroupName,VW_Consumption_STK_FG.itemname ,9 as OtherT,ITEMCODE
		FROM VW_Consumption_STK_FG with(nolock) 
		inner join @companyname t on VW_Consumption_STK_FG.companyname = t.StringValue
		where  Deptt in ('RM','SF','FG','RM Consumables') 
		GROUP BY companyname,sysdate,Deptt,GroupName,SubGroupName,VW_Consumption_STK_FG.itemname,ITEMCODE

		UNION ALL

		Select  
		a.companyname,A.sysdate,SUM(A.AcceptedQty) QTY,I.Deptt,I.GroupName ,'Production at JW',i.SubGroupName,
		i.ItemName ,10 as OtherT,a.ItemCode
		from Challan5AInward a with(nolock)  inner join item i with(nolock)  on i.ItemCode=a.Itemcode and i.CompanyName=a.companyname
		inner join @companyname t on a.companyname = t.StringValue
		 where  I.Deptt in ('RM','SF','FG','RM Consumables') 
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
		 where  ITEM.Deptt in ('RM','SF','FG','RM Consumables') 
		GROUP BY Challan5AInward.companyname,Challan5AInward.sysdate,ITEM.Deptt,ITEM.GroupName, item.SubGroupName,item.ItemName
		,Challan5AInward.Itemcode

 
		--end 13.11.2021
		UNION ALL

		select  V.ProcessorName ,V.DATE,SUM(v.Qty) AS QTY,  i.Deptt,i.GroupName,'Pord. of JW',i.SubGroupName,i.ItemName 
		,12 as OtherT,v.ItemCode
		from Despatch.DBO.vw_SubChallanListMulti v with(nolock) 
		inner join MaterialProcessing.dbo.item  i with(nolock)  on i.itemcode=v.Itemcode and i.CompanyName=v.ProcessorName
		inner join Despatch.DBO.vw_subsidiaryChallanItem O with(nolock) on O.ProcessorName=V.ProcessorName and 
		O.MainChallanDate=v.MainChallanDate and O.MainChallanNo=v.MainChallanNo
		inner join MaterialProcessing.dbo.item i1 with(nolock)  on i1.itemcode =o.itemcode and i1.CompanyName=O.ProcessorName
		inner join @companyname t on V.ProcessorName= t.StringValue
		--BY raj 18.11.2021 as discuss not shown same return itemcode
		where O.Itemcode!=v.Itemcode  
		and  i.Deptt in ('RM','SF','FG','RM Consumables') 
		--end by raj
		GROUP BY V.ProcessorName ,V.DATE,i.Deptt,i.GroupName,i.SubGroupName,i.ItemName,v.ItemCode

		UNION ALL

		select  V.ProcessorName ,V.DATE,SUM(v.Qty) AS QTY,  i1.Deptt,i1.GroupName,'Consumption of JW',i1.SubGroupName,i1.ItemName , 
		13 as OtherT,o.ItemCode
		from Despatch.DBO.vw_SubChallanListMulti v with(nolock) 
		inner join MaterialProcessing.dbo.item  i with(nolock)  on i.itemcode=v.Itemcode and i.CompanyName=v.ProcessorName
		inner join Despatch.DBO.vw_subsidiaryChallanItem O with(nolock)  on O.ProcessorName=V.ProcessorName and 
		O.MainChallanDate=v.MainChallanDate and O.MainChallanNo=v.MainChallanNo
		inner join MaterialProcessing.dbo.item i1 with(nolock)  on i1.itemcode =o.itemcode and i1.CompanyName=O.ProcessorName
		inner join @companyname t on V.ProcessorName= t.StringValue
		--BY raj 18.11.2021 as discuss not shown same return itemcode
		left join Despatch.DBO.vw_subsidiaryChallanItem L with(nolock)  on L.ProcessorName=V.ProcessorName and 
		L.MainChallanDate=v.MainChallanDate and L.MainChallanNo=v.MainChallanNo
		where    L.Itemcode!=v.Itemcode 
		and  i1.Deptt  in ('RM','SF','FG','RM Consumables') 
		--end 18.11.2021
		GROUP BY  V.ProcessorName ,V.DATE,i1.Deptt,i1.GroupName,i1.SubGroupName,i1.ItemName,o.ItemCode

		--added Stock Adju entry 
		union All

		select p.vCompanyName,p.dSysdate, sum(fPendingQty) qty ,i.Deptt,i.GroupName,'Stock Adjustment Entry' ,i.SubGroupName,i.ItemName,
		13 as OtherT,p.FGITEMCODE
		from Prod_RMD_InOut p with(nolock) inner join item i with(nolock) on 
		I.ItemCode=p.FGITEMCODE and i.CompanyName=p.vCompanyName
		where   vToGodown='Stock Adjustment Entry'  and  i.Deptt  in ('RM','SF','FG','RM Consumables') 
		group by  p.vCompanyName,p.dSysdate,i.Deptt,i.GroupName,i.SubGroupName,i.ItemName,p.FGITEMCODE 
				 
		union all

		 
		select  v.CompanyName,sysdate,SUM(qty) AS QTY,item.Deptt,item.GroupName,'Stock Adjustment Entry' , item.SubGroupName,item.ItemName,
		13 as OtherT,V.ItemCode
		from WarehousetoWareHouse V  with(nolock)  inner join
		WareHouse W   with(nolock)  on W.ItemCode=V.ItemCode and w.CompanyName=v.CompanyName and w.WareHouseName=v.ToWareHouse
		inner join item with(nolock) on item.ItemCode=v.ItemCode and item.CompanyName=v.CompanyName
		where ToWareHouse like 'Stock Adjustment Entry' 
		  and  item.Deptt  in ('RM','SF','FG','RM Consumables')
		 GROUP BY  v.CompanyName,sysdate,item.Deptt,item.GroupName, item.SubGroupName,item.ItemName,V.ItemCode

		) A 
		--inner join FactoryInfo F with(nolock) on F.Name=A.CompanyName change on 3.12.2021
		inner join @companyname t on A.CompanyName = t.StringValue
		where    
		A.SysDate >=@PARADATEFROM  
		
		union all
		 --Opending calculation base on STK In Hand 
		 select companyname,DATEADD(D,-1,@PARADATEFROM),Deptt,isnull(GroupName,''),isnull(SubGroupName,''), stkinhand,'Op.Factory Owned',ItemName,ItemCode
		 from item with(nolock) inner join @companyname C on C.StringValue=item.CompanyName
		 WHERE  Deptt in ('RM','SF','FG','RM Consumables')

		 union all
		 
		 select WareHouse.companyname,DATEADD(D,-1,@PARADATEFROM),WareHouse.Deptt,isnull(WareHouse.GroupName,''),isnull(Item.SubGroupName,''), 
		 WareHouse.stkinhand,'Op.Factory Owned',WareHouse.ItemName,WareHouse.ItemCode
		 from WareHouse with(nolock) inner join @companyname C on C.StringValue=WareHouse.CompanyName
		 inner join item WITH(NOLOCK) on item.CompanyName=WareHouse.CompanyName and item.ItemCode=WareHouse.ItemCode
		 where warehousename not in ('Despatch Godown','Stock Adjustment Entry')  


		


		 --END 

		  DECLARE  @t table (companyname varchar(150),sysdate date,MainGroup varchar(50),GroupName varchar(50),SubGroupName varchar(50),
		 ItemName varchar(600) ,qty float,[Type] varchar(50), ItemCode varchar(50)) 

		 insert into @t 

		 --Op.Factory Owned 
		  --Op.Factory Owned 
		 select  companyname,@PARADATEFROM,MainGroup,isnull(GroupName,''),isnull(SubGroupName,''),isnull(ItemName,''),	 
		 format(round(
		 ISNULL(sum([Op.Factory Owned]),0) -
		 (ISNULL(sum([Branch Tfr Recd]),0) + 
		 ISNULL(sum([Purchase]),0) + ISNULL(SUM([Warehouse Inwards]),0) + ISNULL(sum([Recd from JW (Own)]),0) 		 )
		 +
		 ( ISNULL(sum([Sales]),0) +		 ISNULL(sum([Br Transfer Sent]),0) +		 ISNULL(sum([Sent for JW (Own)]),0) + 
		 ((ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0))) +		 ISNULL(sum([Stock Adjustment Entry]),0)) 
		 -
		 ((ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0))) ,@qtyPar),'0.000') 		 as [Cl.Factory Owned],

		 'Op.Factory Owned' as [Type],A.ITEMCODE
		 from @temp   
		pivot
		(

		sum(Qty) for Type in ([Op.Factory Owned],[Purchase],[Warehouse Inwards],[Branch Tfr Recd],[Recd from JW (Own)], [Sales],
		[Br Transfer Sent],[Sent for JW (Own)], [Total Production Own+JW],[Total Consumption Own+JW],
		 [Pord. of JW],[Consumption Of JW],[Stock Adjustment Entry])
		) A 
		where a.MainGroup in ('RM','FG','SF','RM Consumables' )
		group by  A.CompanyName,  a.MainGroup,GroupName,a.SubGroupName,A.ItemName,a.itemcode 

		-- we have another function for Op.Factory JW (Others)
		--union all

		-- select  companyname,@PARADATEFROM,MainGroup,isnull(GroupName,''),isnull(SubGroupName,''),isnull(ItemName,''),	 
		-- isnull(sum([Recd For JW (Others)]),0)	-
		-- isnull(sum([Return JW(Others)]),0) -
		-- isnull(sum([Consumption Of JW]),0) + 
		-- isnull(sum([Production Of JW]),0) as [Cl.Factory JW (Others)], 'Op.Factory JW (Others)' as [Type],A.ITEMCODE
		 
		--from @temp   
		 
		--pivot
		--(

		--sum(Qty) for Type in ([Recd For JW (Others)],[Return JW(Others)],[Consumption Of JW],[Production Of JW])
		--) A 
		--where a.MainGroup in ('RM','FG','SF','RM Consumables' )
		--and sysdate<=@Paraopdate
		--group by  A.CompanyName,  a.MainGroup,GroupName,a.SubGroupName,A.ItemName,a.itemcode 

 

		  


		 

		--select  companyname,DATEADD(D,-1,@PARADATEFROM),MainGroup,GroupName,SubGroupName,ItemName,	 
		-- format(
		-- round(isnull(sum([Op.At JW (Own)]),0)  - 
		-- ISNULL(sum([Recd from JW (Own)]),0)+ 
		-- ISNULL(sum([Sent for JW (Own)]),0)+ISNULL(sum([Production at JW]),0) -  ISNULL(sum([Consumption At JW]),0) ,@qtyPar),'0.000')  as [Cl.At JW (Own)] 
		 
		-- ,'Op.Factory JW (Own)' as [Type],A.ITEMCODE
		-- from @temp   
		--pivot
		--(
		--sum(Qty) for Type in ([Op.At JW (Own)], [Recd from JW (Own)]	,[Sent for JW (Own)],[Consumption At JW],[Production at JW])
		--) A 
		--where a.MainGroup in ('RM','FG','SF','RM Consumables')
		--group by  A.CompanyName,  a.MainGroup,GroupName,a.SubGroupName,A.ItemName,a.itemcode 

		SELECT T.companyname,T.sysdate,MainGroup,GroupName,SubGroupName,qty,Type,ItemName,0,0,T.ItemCode 
		FROM @t T where isnull(T.qty,0)!=0

	 
 
 
END






