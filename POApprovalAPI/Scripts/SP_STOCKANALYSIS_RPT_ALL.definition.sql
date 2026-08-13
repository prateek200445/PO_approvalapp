

 

CREATE PROCEDURE [dbo].[SP_STOCKANALYSIS_RPT_ALL]
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
	


		declare @TempCompany varchar(max)
		select @TempCompany = STUFF((SELECT ',' +  (StringValue) 
                    from @companyname
                    group by StringValue 
                    order by StringValue
            FOR XML PATH(''), TYPE
            ).value('.', 'NVARCHAR(MAX)') 
        ,1,1,'') 

		

		declare @openingdate date = (select min(OpeningBalanceDate) from OpeningBalanceCommoditywise)
		declare @qtyPar int  =3--(select value from Loginentry.dbo.erp_setting where name='qty') 
	    declare @ParaDATEFROM DATE =@DATEFROM
		declare @ParaDATEto DATE =@DATEto
		declare @ParaRptType int =@RptType
		declare @Paraopdate DATE  = dateadd(DD,-1, @DATEFROM)

		declare @temp table (companyname varchar(150),sysdate date,MainGroup varchar(50),GroupName varchar(50),
		SubGroupName varchar(50),Qty float ,Type varchar(50),ItemName varchar(500) ,OrderType int ,Sort int,itemcode varchar(50),ItemDtl varchar(50) )

		declare @optemp table (companyname varchar(150),sysdate date,MainGroup varchar(50),GroupName varchar(50),
		SubGroupName varchar(50),Qty float ,Type varchar(50),ItemName varchar(500) ,OrderType int ,Sort int,itemcode varchar(50) )
		
		insert into @optemp 
		select companyname,sysdate,A.Deptt,GroupName,SubGroupName,A.BalanceQty,Type,ItemName,0,0,ItemCode 
		from dbo.[FN_STOCKANALYSIS_RPT_ALL_OP] ( @TempCompany,@ParaDATEFROM ,@RptType,@intOp) A

		--insert into @optemp 		 
		--EXEC [dbo].[SP_STOCKANALYSIS_RPT_ALL_OP]  @companyname,@ParaDATEFROM ,@ParaDATEFROM		 		 

		 --FOR JOB WORK OWN OPENING
		 INSERT INTO @optemp
		 select CompanyName  ,@Paraopdate  ,Deptt  ,Groupname  ,SubGroupname  , BalanceQty,'Op.At JW (Own)', ItemName,0,0,itemcode 
		 from Despatch.[dbo].[fn_JobWork_Own_op] (@ParaDATEFROM,@TempCompany)
		 inner join @companyname C on C.StringValue=CompanyName

		  --FOR JOB WORK OTHER OPENING

		  if(@TempCompany = 'HCP Plastene Bulkpack Ltd') 
		  begin
				 INSERT INTO @optemp
				 select CompanyName  ,@Paraopdate  ,MainGroup  ,Groupname  ,SubGroupname  , Qty,'Op.Factory JW (Others)', ItemName,0,0,itemcode 
				 from Despatch.dbo.fn_JobWork_Other (@ParaDATEFROM,@ParaDATEFROM,@TempCompany)
				 inner join @companyname C on C.StringValue=CompanyName
		 end
		 else
		 begin
		    INSERT INTO @optemp
			 select CompanyName  ,@Paraopdate  ,MainGroup  ,Groupname  ,SubGroupname  , Qty,'Op.Factory JW (Others)', ItemName,0,0,itemcode 
			 from Despatch.dbo.fn_JobWork_Other_thanHCP (@ParaDATEFROM,@ParaDATEFROM,@TempCompany)
			 inner join @companyname C on C.StringValue=CompanyName
		 end

		 --ADDED PROD/CON FIRST IN TEMP TABLE
		 
		 declare @sql NVARCHAR(MAX)
		 SET @sql ='	 
			select 
			Companyname,sysdate,deptt,GroupName,SubGroupName,  (QTY) AS QTY,Type,itemname,OtherT,			
			case when deptt=''RM'' then 1 when deptt=''SF'' then 2 when deptt=''FG'' then 3 else 4 end Srno,itemcode,0 as Itemdt
			from (
			select Deptt,round(qty,2) as QTY,GroupName,SubGroupName,ITEMNAME,ItemCode, ''Total Production Own+JW'' Type ,8 as OtherT,sysdate,companyname
			from vw_production_stk_FG with(nolock) where (Deptt =''RM'' OR Deptt =''SF'' OR Deptt =''FG'') AND companyname in (''' + @TempCompany +''') and sysdate between ''' 
			+ CONVERT(char(10),@DATEFROM,120) + ''' and ''' + CONVERT(char(10),@DATEto,120) + '''
			union all
			select Deptt, round(qty,2) as QTY ,GroupName, SubGroupName,ITEMNAME,ItemCode, ''Total Consumption Own+JW'' Type ,9 as OtherT,sysdate,companyname
			from vw_consumption_stk_FG  with(nolock) where (Deptt =''RM'' OR Deptt =''SF'' OR Deptt =''FG'') AND companyname in (''' + @TempCompany +''') and sysdate between  ''' 
			+ CONVERT(char(10),@DATEFROM,120) + ''' and ''' + CONVERT(char(10),@DATEto,120) + '''
			 ) A 
			 --GROUP BY Companyname,sysdate,deptt,GroupName,SubGroupName,Type,itemname,OtherT,itemcode 
			 ' 

		  insert into @temp EXECUTE sp_executesql  @sql;


		-- SELECT  companyname,sysdate,Deptt,GroupName,
		--SubGroupName,SUM(qty) QTY,'Total Production Own+JW' ,ITEMNAME ,8 as OtherT,0,ITEMCODE
		--FROM VW_PRODUCTION_STK_FG [VW_PRODUCTION_STK] with(nolock) --change VW_PRODUCTION_STK to VW_PRODUCTION_STK_FG 20.04.2022
		--inner join @companyname t on [VW_PRODUCTION_STK].companyname = t.StringValue
		--where CAST( sysdate AS DATE) between @DATEFROM and @DATEto 
		----and Deptt in ('RM','SF','FG','RM Consumables')  
		--GROUP BY companyname,sysdate,Deptt,GroupName,SubGroupName,ITEMNAME,ITEMCODE

		--UNION ALL
		--SELECT  companyname,sysdate,Deptt,GroupName,
		--SubGroupName,SUM(qty) QTY,'Total Consumption Own+JW',VW_Consumption_STK.itemname ,9 as OtherT,0,ITEMCODE
		--FROM VW_Consumption_STK_FG VW_Consumption_STK with(nolock)  --change VW_Consumption_STK to VW_Consumption_STK_FG 20.04.2022
		--inner join @companyname t on VW_Consumption_STK.companyname = t.StringValue
		--where CAST( sysdate AS DATE) between @DATEFROM and @DATEto 
		----and  Deptt in ('RM','SF','FG','RM Consumables') 
		--GROUP BY companyname,sysdate,Deptt,GroupName,SubGroupName,VW_Consumption_STK.itemname,ITEMCODE


		 --END
		insert into @temp
		select 
		companyname,CAST(sysdate AS DATE),MainGroup,A.GroupName,
		case when isnull(SubGroupName,'')='' then A.GroupName else isnull(SubGroupName,'') end SubGroupName, (Qty) AS Qty ,Type ,
		ItemName ,OrderT,case when MainGroup='RM' then 1 when MainGroup='SF' then 2 when MainGroup='FG' then 3 else 4 end as Sort,
		ItemCode,MRNo 
		from (
		select companyname,sysdate,sum(Qty) as Qty,MainGroup,GroupName,Type,SubGroupName,ItemName,OrderT,ItemCode,MRNo		
		from 
		(
		select 	companyname,sysdate,MRNo,SRNO,
		
		case when unit != 'KGS' then isnull((netwt),0) else isnull((acceptedqty),0) end as qty,

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
		and  itemDeptt  in ('RM','SF','FG','RM Consumables') 
		and 		sysdate between @DATEFROM and @DATEto
		--and Categoryseries ='JBIN-OT'
		) a  group by  CompanyName,SysDate, MainGroup,GroupName,Type,SubGroupName,ItemName,OrderT,ItemCode,MRNo	

		union all

		select CompanyName ,SysDate,case when Unit != 'KGS' then isnull(sum(netwt),0) else isnull(sum(acceptedqty),0) end as qty,
		Vw_StoreInwards.itemDeptt as MainGroup,Vw_StoreInwards.GroupName , 
		case when  FirmGSTIn= VendorGST then 'Branch Tfr Recd'  	
			when Categoryseries ='JBIN-SE' then 'Recd from JW (Own)' 	
			when Categoryseries ='JBIN-OT' then 'Recd For JW (Others)' 
			else   'Purchase'   end as Type ,SubGroupName,ItemName ,

			case when  FirmGSTIn= VendorGST then 1	
			when Categoryseries ='JBIN-SE' then 2	
			when Categoryseries ='JBIN-OT' then 4 	
			else   2   end as OrderType,ItemCode,MRNo


		from Vw_StoreInwards  with(nolock) inner join @companyname t on CompanyName = t.StringValue 
		where   
		Category   in ('JOB IN') and Cancel !='Cancelled' and  itemDeptt  in ('RM','SF','FG','RM Consumables')
		and 		sysdate between @DATEFROM and @DATEto
	--	and Categoryseries ='JBIN-OT'
		group by SysDate,CompanyName,GroupName ,itemDeptt ,VendorGST,Categoryseries,FirmGSTIn,SubGroupName,ItemName,ItemCode,MRNo,Unit

  ---- added by manish on 16th Sep 2023
		--union all

		--select  	v.ProcessorName as CompanyName,v.MainChallanDate
		--,sum(V.OrderQty) ,I.Deptt,i.GroupName,'Recd For JW (Others)' Type,
		--i.SubGroupName ,i.ItemName , 4 OrderType,v.ItemCode,''
		--from   Despatch.DBO.vw_subsidiaryChallanItem v with(nolock)  		
		--inner join MaterialProcessing.DBO.item i on V.Itemcode=i.ItemCode and i.CompanyName=v.ProcessorName 
		--where  CAST( V.MainChallanDate AS DATE) between @DATEFROM and @DATEto
		--and ISNULL(v.isfreeze,0) = 0 and  i.Deptt  in ('RM','SF','FG','RM Consumables')
		--group by v.ProcessorName ,v.MainChallanDate
		-- ,I.Deptt,i.GroupName,		i.SubGroupName ,i.ItemName ,v.ItemCode
	--- end	
		
		union all


		select v.CompanyName ,InwardDate,sum(qty),W.Deptt as  MaingroupName,W.GroupName,'Warehouse Inwards' Type,
		W.SubGroupName,V.ItemName,2 OrderType,w.ItemCode,''
		from WareHouseInwards V with(nolock) inner join
		warehouse W with(nolock)  on W.ItemCode=V.ItemCode and w.CompanyName=v.CompanyName and w.WareHouseName=v.ToWareHouse
		   
		inner join @companyname t on v.CompanyName = t.StringValue
		where v.transid =0 and  W.Deptt  in ('RM','SF','FG','RM Consumables')
		and 		InwardDate between @DATEFROM and @DATEto
		GROUP BY W.GroupName,v.InwardDate, v.CompanyName,W.Deptt,W.SubGroupName,V.ItemName,w.ItemCode



		union all

		select PurchaseVoucherItem.CompanyName ,PurchaseVoucher.SysDate,
		case when per != 'KGS' then isnull(sum(netwt),0) else isnull(sum(ActualQty),0) end as qty,

		--,sum(ActualQty) as ActualQty
		item.Deptt as  MainGroupName,item.GroupName , 
		case when F.NewGSTNo=L.NewGSTNo then  'Branch Tfr Recd'  
		when PurchaseVoucher.VoucherType='Job Invoice' 
		then 'Recd from JW (Own)' else 'Purchase' end ,item.SubGroupName ,ITEM.ItemName ,
		case when F.NewGSTNo=L.NewGSTNo then  1 	when PurchaseVoucher.VoucherType='Job Invoice' then 3 else 2 end as OtherT,
		PurchaseVoucherItem.ItemCode,PurchaseVoucher.StoreInwardNo
		from PurchaseVoucherItem with(nolock)  
		inner join item with(nolock)  on item.CompanyName=PurchaseVoucherItem.CompanyName and 		item.ItemCode=PurchaseVoucherItem.ItemCode 
		inner join PurchaseVoucher with(nolock)  on 
		PurchaseVoucher.StoreInwardNo=PurchaseVoucherItem.StoreInwardNo and 
		PurchaseVoucherItem.CompanyName=PurchaseVoucher.CompanyName 
		inner join ledgermaster L with(nolock) On L.CompanyName=PurchaseVoucherItem.CompanyName and 
		l.LedgerName=PurchaseVoucher.SupplierName
		inner join FactoryInfo f with(nolock)  on F.Name=PurchaseVoucher.CompanyName and f.SrNo=PurchaseVoucher.companyId
		inner join @companyname t on PurchaseVoucher.CompanyName = t.StringValue
		where PurchaseVoucher.SysDate between @DATEFROM and @DATEto and  item.Deptt  in ('RM','SF','FG','RM Consumables')
		and
		PurchaseVoucher.StoreInwardNo in (
		select distinct p.StoreInwardNo from PurchaseVoucherItem p with(nolock)  inner join PurchaseVoucher V with(nolock)  
		on p.StoreInwardNo=v.StoreInwardNo and 
		p.CompanyName=v.CompanyName 
		except
		select 	distinct SrNo
		from Vw_StoreInwards  with(nolock)  where    Vw_StoreInwards.Cancel !='Cancelled' --and Category not in ('JOB IN')
		) group by PurchaseVoucherItem.CompanyName ,PurchaseVoucher.SysDate,item.Deptt,item.GroupName,l.NewGSTNo,VoucherType,
		F.NewGSTNo,item.SubGroupName,ITEM.ItemName,PurchaseVoucherItem.ItemCode,PurchaseVoucher.StoreInwardNo,PurchaseVoucherItem.Per

		union all
		---- sales voucher data---
		select I.companyname,S.InvDate AS DespatchDate
		-- comment by manish on 18th April 2024
		--, SUM(ActualQty) AS qTY 
		-- end comment
		, SUM(Netwt) AS qTY
		,C.Deptt,C.GroupName, 
		case when F.NewGSTNo=L.NewGSTNo then  'Br Transfer Sent' else 'Sales' end Type,C.SubGroupName,C.ItemName ,
		case when F.NewGSTNo=L.NewGSTNo then  6 else 5 end  as OtherT,I.ITEMCODE,S.InvNo

		from 
		SalesVoucher S with(nolock) inner join SalesVoucherItem I with(nolock) on 
		S.companyId=I.companyId and S.CompanyName=I.CompanyName and S.InvNo=I.InvNo and s.InvDate=i.InvDate and s.InvYear=i.Invyear
		inner join LedgerMaster L with(nolock) On L.CompanyName=S.CompanyName and L.LedgerName=S.BuyerName
		left join Item C with(nolock) on C.CompanyName=I.CompanyName and C.itemcode=I.itemcode
		inner join FactoryInfo F with(nolock) on f.name=s.CompanyName and f.SrNo=S.companyId 
		inner join @companyname t on s.CompanyName = t.StringValue
		WHERE S.InvDate between @DATEFROM and @DATEto and C.Deptt  in ('RM','SF','FG','RM Consumables') and S.VoucherType <>'Job Invoice' --13.10.2021
		GROUP BY  I.companyname,s.InvDate,I.Commodity,s.InvNo,C.Deptt,C.GroupName,l.NewGSTNo,S.VoucherType,F.NewGSTNo,
		C.SubGroupName,C.ItemName,i.ITEMCODE,S.InvNo

		union all

		--- pre sales data which is not reflected in sales voucher 14th July 2022
		select vw_SalesRegister.companyname,vw_SalesRegister.DespatchDate, sum(Netwt),Deptt,i.GroupName, 
		'Sales'  Type,i.SubGroupName,i.ItemName,  5  , vw_SalesRegister.Itemcode,vw_SalesRegister.invno
		from vw_SalesRegister with(nolock)  inner join item i with(nolock)  on vw_SalesRegister.Itemcode = i.ItemCode 
		inner join @companyname t on vw_SalesRegister.companyname = t.StringValue
		and vw_SalesRegister.companyname = i.CompanyName

		where   CONVERT(char(10),DespatchDate, 120) 
		 >= @openingdate and vw_SalesRegister.Invno not in(select Invno from MaterialProcessing..salesvoucher where vw_SalesRegister.aresuffix = InvYear 
                            and vw_SalesRegister.companyname = MaterialProcessing..salesvoucher.companyname and InvNo is not null
                        		union select InwardNo
		                        from Journal
		                        where vw_SalesRegister.aresuffix=Journal.yr and vw_SalesRegister.companyname=Journal.CompanyName and InwardNo is not null
                        ) 
				group by vw_SalesRegister.companyname,vw_SalesRegister.DespatchDate,Deptt,i.GroupName, i.SubGroupName,i.ItemName,  vw_SalesRegister.Itemcode,vw_SalesRegister.invno
        ---end pre sales data which is not reflected in sales voucher 14th July 2022
		union all
		--FORMAT SYSDATE TO ONLY DATE 
		select vw_challan5a.companyname ,CAST(sysdate AS DATE) AS SYSDATE,sum(ItemQty)'Qty',vw_challan5a.Deptt,vw_challan5a.GroupName,
		case when vw_challan5a.NewGSTNo=[factoryGSTNo] then 'Br Transfer Sent' else 'Sent for JW (Own)' end Type ,
		vw_challan5a.SubGroupName,vw_challan5a.ITEMNAME ,
		case when vw_challan5a.NewGSTNo=[factoryGSTNo] then 6 else 7 end  as OtherT,vw_challan5a.ItemCode,vw_challan5a.ChallanNo
		from Despatch.dbo.vw_challan5a  with(nolock) 
		
		inner join @companyname t on vw_challan5a.companyname = t.StringValue
		where CAST(sysdate AS DATE) between @DATEFROM and @DATEto and vw_challan5a.Deptt in ('RM','SF','FG','RM Consumables') and (iscancel is null or iscancel = '') 
		group by vw_challan5a.companyname ,sysdate,  vw_challan5a.Deptt,vw_challan5a.GroupName,vw_challan5a.NewGSTNo,
		[factoryGSTNo],vw_challan5a.SubGroupName,vw_challan5a.ITEMNAME,vw_challan5a.ItemCode,vw_challan5a.ChallanNo

		union all
		select f.ProcessorName, f.Date,sum(Qty) qty , f.Deptt,F.GroupName,'Return JW(Others)',
		F.SubGroupName,F.ItemName ,8 as OtherT,f.ItemCode,f.MainChallanNo
		from Despatch.dbo.vw_SubChallanListMulti f with(nolock) 		
		inner join @companyname t on f.ProcessorName = t.StringValue
		----- added by manish on 9th Sep 2023
		inner join Despatch.dbo.[vw_subsidiaryChallanItem] VS on VS.MainChallanNo = f.mainchallanno
		and VS.MainChallanDate = f.MainChallanDate
		and VS.SubsidiaryBuyer = f.SubsidiaryBuyer
		--and f.commodityname = VS.commodityname
		----
		where CAST( f.Date AS DATE) between @DATEFROM and @DATEto and F.Deptt in ('RM','SF','FG','RM Consumables') 
		and ( isnull(f.iscancel,'no')='no' or  isnull(f.iscancel,'')='') --09.Mar.2022
		--and f.MainChallanNo + format(f.MainChallanDate,'MM/dd/yyyy')+ f.SubsidiaryBuyer
		-- comment on 14th April 2024 		and isnull(Vs.isfreeze,0) =0 -- end comment 14th April 2024 
		--in (select  MainChallanNo + format(MainChallanDate,'MM/dd/yyyy')+ SubsidiaryBuyer 
		--from Despatch.dbo.[vw_subsidiaryChallanItem] where  ISNULL(isfreeze,0) = 0 )  
		group by f.Date, F.GroupName, f.ProcessorName,F.Deptt,F.GroupName,f.SubGroupName,F.ItemName,f.ItemCode,f.MainChallanNo

		union all
		

		--- for internal stock JV added by manish on 13th Sep 2023
			--- change by manish on 28th April 2025 adding condition of itemmaster
		select stockjv.companyname,sysdate,qty,item.deptt,item.groupname,type,item.subgroupname,item.itemname,srno,stockjv.itemcode,challanno from stockjv
			inner join @companyname t on stockjv.companyname = t.StringValue
		inner join item on item.ItemCode = StockJV.itemcode and item.CompanyName = StockJV.companyname
		where sysdate <= @DATEto
		--- end comment

		UNION ALL

		Select  
		a.companyname,A.sysdate,SUM(A.AcceptedQty) QTY,I.Deptt,I.GroupName ,'Production at JW',i.SubGroupName,
		i.ItemName ,10 as OtherT,a.ItemCode,a.ChallanSubCode
		from Challan5AInward a with(nolock)  inner join item i with(nolock)  on i.ItemCode=a.Itemcode and i.CompanyName=a.companyname
		inner join @companyname t on a.companyname = t.StringValue
		--where Deptt not in ('RM','RM Consumables') -- added 13.10.2021
		where CAST( A.sysdate AS DATE) between @DATEFROM and @DATEto and  I.Deptt in ('RM','SF','FG','RM Consumables') 
		GROUP BY a.companyname,A.sysdate,I.Deptt,I.GroupName,i.SubGroupName,i.ItemName,a.ItemCode,a.ChallanSubCode

		UNION ALL
		--13.11.2021
		 Select Challan5AInward.companyname,Challan5AInward.sysdate,SUM(Challan5AInward.AcceptedQty) QTY,
		ITEM.Deptt,ITEM.GroupName ,'Consumption At JW',
		item.SubGroupName,item.ItemName ,11 as OtherT
		,ChallanItem.Itemcode,Challan5AInward.ChallanSubCode
		from Challan5AInward with(nolock)  INNER JOIN Despatch..Vw_Challan5A ChallanItem with(nolock)  on 
		Challan5AInward.ChallanSubCode=ChallanItem.ChallanNo + '/' + cast(ChallanItem.SubCode as varchar(2))						
		INNER JOIN ITEM with(nolock)  ON ITEM.ItemCode=ChallanItem.ItemCode AND ITEM.CompanyName=Challan5AInward.companyname
		inner join @companyname t on Challan5AInward.companyname = t.StringValue
		where CAST( Challan5AInward.sysdate AS DATE) between @DATEFROM and @DATEto and ITEM.Deptt in ('RM','SF','FG','RM Consumables') 
		GROUP BY Challan5AInward.companyname,Challan5AInward.sysdate,ITEM.Deptt,ITEM.GroupName, item.SubGroupName,item.ItemName
		,ChallanItem.Itemcode,Challan5AInward.ChallanSubCode

		--Select Challan5AInward.companyname,Challan5AInward.sysdate,SUM(Challan5AInward.AcceptedQty) QTY,
		--ITEM.Deptt,ITEM.GroupName ,'Consumption At JW',
		--item.SubGroupName,item.ItemName ,11 as OtherT,Challan5AInward.Itemcode
		--from Challan5AInward INNER JOIN Despatch..ChallanItem on Challan5AInward.ChallanSubCode=ChallanItem.Code 
		--OR Challan5AInward.CombineChallanNo=ChallanItem.Code 
		--INNER JOIN Despatch..Challan5A on ChallanItem.Code=Challan5A.ChallanNo 
		--INNER JOIN Despatch.dbo.CompanyMaster ON dbo.Challan5AInward.BuyerName = Despatch.dbo.CompanyMaster.CompanyName INNER JOIN
		--dbo.FactoryInfo ON Challan5AInward.companyname = dbo.FactoryInfo.Name
		--INNER JOIN ITEM ON ITEM.ItemCode=ChallanItem.ItemCode AND ITEM.CompanyName=Challan5AInward.companyname
		--inner join @companyname t on Challan5AInward.companyname = t.StringValue
		--GROUP BY Challan5AInward.companyname,Challan5AInward.sysdate,ITEM.Deptt,ITEM.GroupName, item.SubGroupName,item.ItemName
		--,Challan5AInward.Itemcode
		--end 13.11.2021
		
		-- change condition for RM and other deptt in prod of JW as per request of Mr. ANil goyal on dated 8th Dec 2022
		UNION ALL

		select  V.ProcessorName ,V.DATE,SUM(v.Qty) AS QTY,  v.Deptt,V.GroupName,'Pord. of JW',V.SubGroupName,V.ItemName 
		,12 as OtherT,v.ItemCode,v.MainChallanNo
		from Despatch.DBO.vw_SubChallanListMulti v with(nolock) 		
		inner join Despatch.DBO.vw_subsidiaryChallanItem O with(nolock) on O.ProcessorName=V.ProcessorName and 
		O.MainChallanDate=v.MainChallanDate and O.MainChallanNo=v.MainChallanNo
		-- comment by manish on 26th July 2025
		--inner join MaterialProcessing.dbo.item i1 with(nolock)  on i1.itemcode =o.itemcode and i1.CompanyName=O.ProcessorName
		--end comment

		inner join @companyname t on V.ProcessorName= t.StringValue
		where CAST( V.DATE AS DATE) between @DATEFROM and @DATEto and  O.SubGroupName!=v.SubGroupName and V.Deptt = 'RM'
			and ISNULL(O.isfreeze,0) = 0 
		GROUP BY V.ProcessorName ,V.DATE,V.Deptt,v.GroupName,v.SubGroupName,v.ItemName,v.ItemCode,v.MainChallanNo


		--UNION ALL

		--select  'HCP Plastene Bulkpack Ltd' ,'2023-04-01',8000,  'RM'
		--,'Granules','Consumption of JW','PP Granules','PP IOCL 1030RG' 
		--,13 as OtherT,'RAW05155',''
		
		--UNION ALL

		--select  'HCP Plastene Bulkpack Ltd' ,'2023-04-01',8000,  'SF'
		--,'PP/PE Fabric','Pord. of JW','PP UL Fabric','PP UL Fabric Without UV' 
		--,12 as OtherT,'WIP00024',''
		

		UNION ALL

		select  V.ProcessorName ,V.DATE,SUM(v.Qty) AS QTY,  v.Deptt,V.GroupName,'Pord. of JW',V.SubGroupName,V.ItemName 
		,12 as OtherT,v.ItemCode,v.MainChallanNo
		from Despatch.DBO.vw_SubChallanListMulti v with(nolock) 		
		inner join Despatch.DBO.vw_subsidiaryChallanItem O with(nolock) on O.ProcessorName=V.ProcessorName and 
		O.MainChallanDate=v.MainChallanDate and O.MainChallanNo=v.MainChallanNo
		--and v.commodityname = o.commodityname
		-- comment by manish on 26th July 2025
		--inner join MaterialProcessing.dbo.item i1 with(nolock)  on i1.itemcode =o.itemcode and i1.CompanyName=O.ProcessorName
	     -- end comment
	inner join @companyname t on V.ProcessorName= t.StringValue
		where CAST( V.DATE AS DATE) between @DATEFROM and @DATEto and  O.Itemcode!=v.Itemcode
		and V.Deptt in ('SF','FG','RM Consumables')
			and ISNULL(O.isfreeze,0) = 0 
		GROUP BY V.ProcessorName ,V.DATE,V.Deptt,v.GroupName,v.SubGroupName,v.ItemName,v.ItemCode,v.MainChallanNo
		-- end change condition

		UNION ALL

		select  V.ProcessorName ,V.DATE,SUM(v.Qty) AS QTY,  O.Deptt,O.GroupName,'Consumption of JW',O.SubGroupName,O.ItemName , 
		13 as OtherT,o.ItemCode,v.MainChallanNo
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
	
		where  CAST( V.DATE AS DATE) between @DATEFROM and @DATEto
		and  O.SubGroupName!=v.SubGroupName
		and  L.Itemcode!=v.Itemcode and O.Deptt in ('RM','SF','FG','RM Consumables')
			and ISNULL(O.isfreeze,0) = 0 
		--end 18.11.2021
		GROUP BY  V.ProcessorName ,V.DATE,O.Deptt,O.GroupName,O.SubGroupName,O.ItemName,o.ItemCode,v.MainChallanNo

		--added Stock Adju entry 
		union All

		select p.vCompanyName,p.dSysdate, sum(fPendingQty) qty ,i.Deptt,i.GroupName,'Stock Adjustment Entry' ,i.SubGroupName,i.ItemName,
		13 as OtherT,p.FGITEMCODE,''
		from Prod_RMD_InOut p with(nolock) inner join item i with(nolock) on 
		I.ItemCode=p.FGITEMCODE and i.CompanyName=p.vCompanyName
		where   CAST( p.dSysdate AS DATE) between @DATEFROM and @DATEto and  vToGodown='Stock Adjustment Entry'   and i.Deptt in ('RM','SF','FG','RM Consumables')
		group by  p.vCompanyName,p.dSysdate,i.Deptt,i.GroupName,i.SubGroupName,i.ItemName,p.FGITEMCODE
		 

				 
		union all

		 
		select  v.CompanyName,sysdate,SUM(qty) AS QTY,W.Deptt,W.GroupName,'Stock Adjustment Entry' , W.SubGroupName,W.ItemName,
		13 as OtherT,V.ItemCode,''
		from WarehousetoWareHouse V  with(nolock)  inner join
		WareHouse W   with(nolock)  on W.ItemCode=V.ItemCode and w.CompanyName=v.CompanyName and w.WareHouseName=v.ToWareHouse
		--inner join item with(nolock) on item.ItemCode=v.ItemCode and item.CompanyName=v.CompanyName
		where CAST( Sysdate AS DATE) between @DATEFROM and @DATEto and ToWareHouse = 'Stock Adjustment Entry' and 
		W.Deptt in ('RM','SF','FG','RM Consumables')
		 GROUP BY  v.CompanyName,sysdate,W.Deptt,W.GroupName, W.SubGroupName,W.ItemName,V.ItemCode

		 -- debit note added 27th Sep 2022
		 union all
		select  v.CompanyName,sysdate,-SUM(QtyDifference) AS QTY,W.Deptt,W.GroupName,'Purchase' , W.SubGroupName,W.ItemName,
		13 as OtherT,V.ItemCode,DebitNoteNumber
		from vw_DebitNote V  with(nolock)  inner join
		Item W   with(nolock)  on W.ItemCode=V.ItemCode and w.CompanyName=v.CompanyName 
		--inner join item with(nolock) on item.ItemCode=v.ItemCode and item.CompanyName=v.CompanyName
		inner join @companyname t on W.CompanyName = t.StringValue
		where CAST( Sysdate AS DATE) between @DATEFROM and @DATEto  and 
		W.Deptt in ('RM','SF','FG','RM Consumables') and DebitType = 'Qty Difference'
		 GROUP BY  v.CompanyName,sysdate,W.Deptt,W.GroupName, W.SubGroupName,W.ItemName,V.ItemCode,DebitNoteNumber
		 -- end 

		-- 	 -- credt note added 17th Sep 2024 by manish
		--	 union all
		--select  v.CompanyName,V.invdate,-SUM(QtyDifference) AS QTY,W.Deptt,W.GroupName,'Sales' , W.SubGroupName,W.ItemName,
		--13 as OtherT,VI.ItemCode,V.creditnotenumber
		--from vw_creditnote V with(nolock)
		--inner join CreditNoteItem VI with(nolock) on VI.CreditNoteNumber = V.creditnotenumber 
		--inner join Item W   with(nolock)  on W.ItemCode=VI.ItemCode and w.CompanyName=v.CompanyName 
		----inner join item with(nolock) on item.ItemCode=v.ItemCode and item.CompanyName=v.CompanyName
		--inner join @companyname t on W.CompanyName = t.StringValue
		--where CAST( V.invdate AS DATE) between @DATEFROM and @DATEto  and 
		--W.Deptt in ('RM','SF','FG','RM Consumables') and V.credittype = 'Qty Difference'
		-- GROUP BY  v.CompanyName,V.invdate,W.Deptt,W.GroupName, W.SubGroupName,W.ItemName,VI.ItemCode,V.creditnotenumber
		 -- end 


		) A 
		--inner join FactoryInfo F with(nolock) on F.Name=A.CompanyName change on 3.12.2021
		inner join @companyname t on A.CompanyName = t.StringValue
		where    
		A.SysDate BETWEEN @ParaDATEFROM AND @ParaDATEto and A.Qty!=0 

		
		 
		union all
		select a.companyname,a.sysdate,a.MainGroup,a.GroupName,a.SubGroupName,a.qty,a.Type,a.ItemName,1,
		case when MainGroup='RM' then 1 when MainGroup='SF' then 2 when MainGroup='FG' then 3 else 4 end as Sort
		,a.ITEMCODE,''
		from @optemp a 		 
		where  A.Qty!=0 

		--union all
		--select a.companyname,a.sysdate,a.MainGroup,a.GroupName,a.SubGroupName,a.qty,a.Type,a.ItemName,1,
		--case when MainGroup='RM' then 1 when MainGroup='SF' then 2 when MainGroup='FG' then 3 else 4 end as Sort
		--,a.ITEMCODE 
		--from dbo.[GetOpStkAnaystbl](@companyname,@ParaDATEFROM) a 		 
		--where  A.Qty!=0  
 
		
		 select companyname,sysdate,MainGroup,GroupName,SubGroupName,ItemName,
		 format(ROUND(ISNULL(sum(Qty),0),3),'0.000') as Qty ,Type,itemcode, case when ItemDtl = '0' then '' else ItemDtl end  as Itemdetails  from @temp a 		 
		 GROUP BY GROUPING SETS ((companyname,sysdate,Sort,OrderType,MainGroup,SubGroupName,A.GroupName,Type,ItemName,itemcode,ItemDtl),()) 
		 
		if @intOp=0
		begin
			if @ParaRptType=0
			begin
			select case when A.CompanyName is null then 'Total ' else A.CompanyName end CompanyName , 
			A.MainGroup,A.SubGroupName,
			format(round(ISNULL(sum([Op.Factory Owned]),0),@qtyPar),'0.000') as [Op.Factory Owned],
			format(round(ISNULL(sum([Op.Factory JW (Others)]),0),@qtyPar),'0.000') as [Op.Factory JW (Others)],
			format(round(ISNULL(sum([Op.At JW (Own)]),0),@qtyPar),'0.000') as [Op.At JW (Own)],
			format(round(ISNULL(sum([Branch Tfr Recd]),0),@qtyPar),'0.000') AS [Branch Tfr Recd],
			format(round(ISNULL(sum([Purchase]),0) ,@qtyPar),'0.000')  AS [Purchase],
			format(round(ISNULL(sum([Warehouse Inwards]),0) ,@qtyPar),'0.000')  AS [Warehouse Inwards],		 
			format(round(ISNULL(sum([Recd from JW (Own)]),0),@qtyPar),'0.000')  AS [Recd from JW (Own)],
			format(round(ISNULL(sum([Recd For JW (Others)]),0),@qtyPar),'0.000')  AS [Recd For JW (Others)],
			format(round(ISNULL(sum([Sales]),0),@qtyPar),'0.000')  AS [Sales],
			format(round(ISNULL(sum([Br Transfer Sent]),0),@qtyPar),'0.000')  AS [Br Transfer Sent],
			format(round(ISNULL(sum([Sent for JW (Own)]),0),@qtyPar),'0.000')  AS [Sent for JW (Own)],
			format(round(ISNULL(sum([Return JW(Others)]),0),@qtyPar),'0.000')  AS [Return JW(Others)],
			format(round(ISNULL(sum([Total Production Own+JW]),0),@qtyPar),'0.000')  AS [Total Production Own+JW],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0),@qtyPar),'0.000')  AS [Total Consumption Own+JW],
			format(round(ISNULL(sum([Production at JW]),0),@qtyPar),'0.000')  AS [Production at JW],
			format(round(ISNULL(sum([Consumption At JW]),0),@qtyPar),'0.000')  AS [Consumption At JW] ,
			format(round(ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  AS [Production Of JW],
			format(round(ISNULL(sum([Consumption of JW]),0),@qtyPar),'0.000')  AS [Consumption Of JW] ,
			format(round(ISNULL(sum([Stock Adjustment Entry]),0),@qtyPar),'0.000')  AS [Stock Adjustment Entry] ,		 
			format(round(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Net Production Own],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0),@qtyPar),'0.000')  as [Net Consumption Own],

			format(round(
			ISNULL(sum([Op.Factory Owned]),0)+
			ISNULL(sum([Branch Tfr Recd]),0) +
			ISNULL(sum([Purchase]),0) + 
			ISNULL(sum([Warehouse Inwards]),0) + 
			ISNULL(sum([Recd from JW (Own)]),0) -		 
			ISNULL(sum([Sales]),0)-
			ISNULL(sum([Br Transfer Sent]),0) -
			ISNULL(sum([Sent for JW (Own)]),0) +
			(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0)) -
			(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0)) - 
			ISNULL(sum([Stock Adjustment Entry]),0) ,@qtyPar),'0.000')  as [Cl.Factory Owned],



			format(
			round(
			ISNULL(sum([Op.Factory JW (Others)]),0) +
			ISNULL(sum([Recd For JW (Others)]),0)-
			ISNULL(sum([Return JW(Others)]),0)-
			ISNULL(sum([Consumption of JW]),0) +
			ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)], 

			format(round(
			ISNULL(sum([Op.At JW (Own)]),0)-
			ISNULL(sum([Recd from JW (Own)]),0)+ 
			ISNULL(sum([Sent for JW (Own)]),0) -
			ISNULL(sum([Consumption At JW]),0) +
			ISNULL(sum([Production at JW]),0),@qtyPar),'0.000')  as [Cl.At JW (Own)] 

			--   format(round(0+ISNULL(sum([Recd For JW (Others)]),0)-ISNULL(sum([Return JW(Others)]),0)-ISNULL(sum([Consumption of JW]),0) +ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)], 
			--format(round(0-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)-ISNULL(sum([Consumption At JW]),0) +  		 ISNULL(sum([Consumption At JW]),0) ,@qtyPar),'0.000')  as [Cl.At JW (Own)] 

			from @temp A
 
			pivot
			(
			sum(Qty) for Type in ([Op.Factory Owned],[Op.Factory JW (Others)],[Op.At JW (Own)],[Purchase],[Warehouse Inwards],[Branch Tfr Recd],[Recd from JW (Own)],[Recd For JW (Others)],[Sales],
			[Br Transfer Sent],[Sent for JW (Own)],[Return JW(Others)],[Total Production Own+JW],[Total Consumption Own+JW],
			[Production at JW],[Consumption At JW] ,[Pord. of JW],[Consumption of JW] ,[Stock Adjustment Entry])
			) A
			where a.MainGroup in ('RM','FG','SF'  )
			group by GROUPING SETS ((A.CompanyName, Sort,a.MainGroup,a.SubGroupName),())
			union all
			select case when A.CompanyName is null then 'Total ' else A.CompanyName end CompanyName , 
			A.MainGroup,A.SubGroupName,
			format(round(ISNULL(sum([Op.Factory Owned]),0),@qtyPar),'0.000') as [Op.Factory Owned],
			format(round(ISNULL(sum([Op.Factory JW (Others)]),0),@qtyPar),'0.000') as [Op.Factory JW (Others)],
			format(round(ISNULL(sum([Op.At JW (Own)]),0),@qtyPar),'0.000') as [Op.At JW (Own)],
			format(round(ISNULL(sum([Branch Tfr Recd]),0),@qtyPar),'0.000') AS [Branch Tfr Recd],
			format(round(ISNULL(sum([Purchase]),0) ,@qtyPar),'0.000')  AS [Purchase],
			format(round(ISNULL(sum([Warehouse Inwards]),0) ,@qtyPar),'0.000')  AS [Warehouse Inwards],		 
			format(round(ISNULL(sum([Recd from JW (Own)]),0),@qtyPar),'0.000')  AS [Recd from JW (Own)],
			format(round(ISNULL(sum([Recd For JW (Others)]),0),@qtyPar),'0.000')  AS [Recd For JW (Others)],
			format(round(ISNULL(sum([Sales]),0),@qtyPar),'0.000')  AS [Sales],
			format(round(ISNULL(sum([Br Transfer Sent]),0),@qtyPar),'0.000')  AS [Br Transfer Sent],
			format(round(ISNULL(sum([Sent for JW (Own)]),0),@qtyPar),'0.000')  AS [Sent for JW (Own)],
			format(round(ISNULL(sum([Return JW(Others)]),0),@qtyPar),'0.000')  AS [Return JW(Others)],
			format(round(ISNULL(sum([Total Production Own+JW]),0),@qtyPar),'0.000')  AS [Total Production Own+JW],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0),@qtyPar),'0.000')  AS [Total Consumption Own+JW],
			format(round(ISNULL(sum([Production at JW]),0),@qtyPar),'0.000')  AS [Production at JW],
			format(round(ISNULL(sum([Consumption At JW]),0),@qtyPar),'0.000')  AS [Consumption At JW] ,
			format(round(ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  AS [Production Of JW],
			format(round(ISNULL(sum([Consumption of JW]),0),@qtyPar),'0.000')  AS [Consumption Of JW] ,
			format(round(ISNULL(sum([Stock Adjustment Entry]),0),@qtyPar),'0.000')  AS [Stock Adjustment Entry] ,
			format(round(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Net Production Own],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0),@qtyPar),'0.000')  as [Net Consumption Own],

			format(round(
			ISNULL(sum([Op.Factory Owned]),0)+
			ISNULL(sum([Branch Tfr Recd]),0) +
			ISNULL(sum([Purchase]),0) + 
			ISNULL(sum([Warehouse Inwards]),0)  +
			ISNULL(sum([Recd from JW (Own)]),0) -		 
			ISNULL(sum([Sales]),0)-
			ISNULL(sum([Br Transfer Sent]),0) -
			ISNULL(sum([Sent for JW (Own)]),0) +
			(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0)) -
			(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0)-ISNULL(sum([Stock Adjustment Entry]),0)),@qtyPar),'0.000')  as [Cl.Factory Owned],

			format(round(ISNULL(sum([Op.Factory JW (Others)]),0) +ISNULL(sum([Recd For JW (Others)]),0)-ISNULL(sum([Return JW(Others)]),0)-ISNULL(sum([Consumption of JW]),0) +
			ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)],  		  

			-- comment on manish by 3rd May 2025
			--format(round(ISNULL(sum([Op.At JW (Own)]),0)-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)-ISNULL(sum([Consumption At JW]),0) +  
			--ISNULL(sum([Consumption At JW]),0) ,@qtyPar),'0.000')  as [Cl.At JW (Own)] 
			-- end comment

			format(round(ISNULL(sum([Op.At JW (Own)]),0)-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)-ISNULL(sum([Consumption At JW]),0)  
			+ISNULL(sum([Production at JW]),0),@qtyPar),'0.000') 
			as [Cl.At JW (Own)]

			--format(round(ISNULL(sum([Op.Factory Owned]),0)+ISNULL(sum([Branch Tfr Recd]),0) +ISNULL(sum([Purchase]),0) + ISNULL(sum([Recd from JW (Own)]),0) + ISNULL(sum([Recd For JW (Others)]),0) -ISNULL(sum([Sales]),0)-ISNULL(sum([Br Transfer Sent]),0) -ISNULL(sum([Sent for JW (Own)]),0),@qtyPar),'0.000')  as [Cl.Factory Owned],
			--format(round(0+ISNULL(sum([Recd For JW (Others)]),0)-ISNULL(sum([Return JW(Others)]),0)-ISNULL(sum([Consumption of JW]),0) +ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)], 
			--format(round(0-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)-ISNULL(sum([Consumption At JW]),0) +  
			--ISNULL(sum([Consumption At JW]),0) ,@qtyPar),'0.000')  as [Cl.At JW (Own)] 

			from @temp A
 
			pivot
			(
			sum(Qty) for Type in ([Op.Factory Owned],[Op.Factory JW (Others)],[Op.At JW (Own)],[Purchase],[Warehouse Inwards],[Branch Tfr Recd],[Recd from JW (Own)],[Recd For JW (Others)],[Sales],
			[Br Transfer Sent],[Sent for JW (Own)],[Return JW(Others)],[Total Production Own+JW],[Total Consumption Own+JW],
			[Production at JW],[Consumption At JW] ,[Pord. of JW],[Consumption of JW],[Stock Adjustment Entry])
			) A
			where a.MainGroup in ( 'RM Consumables')
			group by GROUPING SETS ((A.CompanyName, Sort,a.MainGroup,a.SubGroupName),())
			end
			if @ParaRptType=1
			begin 
			select case when A.CompanyName is null then 'Total ' else A.CompanyName end CompanyName , A.MainGroup,
			A.SubGroupName,A.ItemName,
			format(round(ISNULL(sum([Op.Factory Owned]),0),@qtyPar),'0.000') as [Op.Factory Owned],
			format(round(ISNULL(sum([Op.Factory JW (Others)]),0),@qtyPar),'0.000') as [Op.Factory JW (Others)],
			format(round(ISNULL(sum([Op.At JW (Own)]),0),@qtyPar),'0.000') as [Op.At JW (Own)],
			format(round(ISNULL(sum([Branch Tfr Recd]),0),@qtyPar),'0.000') AS [Branch Tfr Recd],
			format(round(ISNULL(sum([Purchase]),0) ,@qtyPar),'0.000')  AS [Purchase],
			format(round(ISNULL(sum([Warehouse Inwards]),0) ,@qtyPar),'0.000')  AS [Warehouse Inwards],		 
			format(round(ISNULL(sum([Recd from JW (Own)]),0),@qtyPar),'0.000')  AS [Recd from JW (Own)],
			format(round(ISNULL(sum([Recd For JW (Others)]),0),@qtyPar),'0.000')  AS [Recd For JW (Others)],
			format(round(ISNULL(sum([Sales]),0),@qtyPar),'0.000')  AS [Sales],
			format(round(ISNULL(sum([Br Transfer Sent]),0),@qtyPar),'0.000')  AS [Br Transfer Sent],
			format(round(ISNULL(sum([Sent for JW (Own)]),0),@qtyPar),'0.000')  AS [Sent for JW (Own)],
			format(round(ISNULL(sum([Return JW(Others)]),0),@qtyPar),'0.000')  AS [Return JW(Others)],
			format(round(ISNULL(sum([Total Production Own+JW]),0),@qtyPar),'0.000')  AS [Total Production Own+JW],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0),@qtyPar),'0.000')  AS [Total Consumption Own+JW],
			format(round(ISNULL(sum([Production at JW]),0),@qtyPar),'0.000')  AS [Production at JW],
			format(round(ISNULL(sum([Consumption At JW]),0),@qtyPar),'0.000')  AS [Consumption At JW] ,
			format(round(ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  AS [Production Of JW],
			format(round(ISNULL(sum([Consumption of JW]),0),@qtyPar),'0.000')  AS [Consumption Of JW] ,
			format(round(ISNULL(sum([Stock Adjustment Entry]),0),@qtyPar),'0.000')  AS [Stock Adjustment Entry] ,
			format(round(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Net Production Own],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0),@qtyPar),'0.000')  as [Net Consumption Own],
		 
		 
			format(
			round(
			ISNULL(sum([Op.Factory Owned]),0)+
			ISNULL(sum([Branch Tfr Recd]),0) +
			ISNULL(sum([Purchase]),0) + 
			isnull(sum([Warehouse Inwards]),0) +
			ISNULL(sum([Recd from JW (Own)]),0) -		 
			ISNULL(sum([Sales]),0)-
			ISNULL(sum([Br Transfer Sent]),0) -
			ISNULL(sum([Sent for JW (Own)]),0) +
			(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0)) -
			(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0))
			-ISNULL(sum([Stock Adjustment Entry]),0),@qtyPar),'0.000')  as [Cl.Factory Owned],

			format(round(
			ISNULL(sum([Op.Factory JW (Others)]),0) +
			ISNULL(sum([Recd For JW (Others)]),0)-
			ISNULL(sum([Return JW(Others)]),0)-
			ISNULL(sum([Consumption of JW]),0) +
			ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)], 

			format(round(
			ISNULL(sum([Op.At JW (Own)]),0)-
			ISNULL(sum([Recd from JW (Own)]),0)+ 
			ISNULL(sum([Sent for JW (Own)]),0) -
			ISNULL(sum([Consumption At JW]),0) +
			ISNULL(sum([Production at JW]),0) 
			
			,@qtyPar),'0.000')  as [Cl.At JW (Own)]  
			from @temp A
 
			pivot
			(
				sum(Qty) for Type in (
				[Op.Factory Owned],
				[Op.Factory JW (Others)],
				[Op.At JW (Own)],[Purchase],[Warehouse Inwards],[Branch Tfr Recd],[Recd from JW (Own)],[Recd For JW (Others)],[Sales],
				[Br Transfer Sent],[Sent for JW (Own)],[Return JW(Others)],[Total Production Own+JW],[Total Consumption Own+JW],
				[Production at JW],[Consumption At JW] ,[Pord. of JW],[Consumption of JW],[Stock Adjustment Entry])
			) A 
			where a.MainGroup in ('RM','FG','SF' )
			group by GROUPING SETS ((A.CompanyName,Sort, a.MainGroup,a.SubGroupName,A.ItemName,a.itemcode),())
			union all
			select case when A.CompanyName is null then 'Total ' else A.CompanyName end CompanyName , A.MainGroup,
			A.SubGroupName,A.ItemName,
			format(round(ISNULL(sum([Op.Factory Owned]),0),@qtyPar),'0.000') as [Op.Factory Owned],
			format(round(ISNULL(sum([Op.Factory JW (Others)]),0),@qtyPar),'0.000') as [Op.Factory JW (Others)],
			format(round(ISNULL(sum([Op.At JW (Own)]),0),@qtyPar),'0.000') as [Op.At JW (Own)],
			format(round(ISNULL(sum([Branch Tfr Recd]),0),@qtyPar),'0.000') AS [Branch Tfr Recd],
			format(round(ISNULL(sum([Purchase]),0) ,@qtyPar),'0.000')  AS [Purchase],
			format(round(ISNULL(sum([Warehouse Inwards]),0) ,@qtyPar),'0.000')  AS [Warehouse Inwards],
			format(round(ISNULL(sum([Recd from JW (Own)]),0),@qtyPar),'0.000')  AS [Recd from JW (Own)],
			format(round(ISNULL(sum([Recd For JW (Others)]),0),@qtyPar),'0.000')  AS [Recd For JW (Others)],
			format(round(ISNULL(sum([Sales]),0),@qtyPar),'0.000')  AS [Sales],
			format(round(ISNULL(sum([Br Transfer Sent]),0),@qtyPar),'0.000')  AS [Br Transfer Sent],
			format(round(ISNULL(sum([Sent for JW (Own)]),0),@qtyPar),'0.000')  AS [Sent for JW (Own)],
			format(round(ISNULL(sum([Return JW(Others)]),0),@qtyPar),'0.000')  AS [Return JW(Others)],
			format(round(ISNULL(sum([Total Production Own+JW]),0),@qtyPar),'0.000')  AS [Total Production Own+JW],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0),@qtyPar),'0.000')  AS [Total Consumption Own+JW],
			format(round(ISNULL(sum([Production at JW]),0),@qtyPar),'0.000')  AS [Production at JW],
			format(round(ISNULL(sum([Consumption At JW]),0),@qtyPar),'0.000')  AS [Consumption At JW] ,
			format(round(ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  AS [Production Of JW],
			format(round(ISNULL(sum([Consumption of JW]),0),@qtyPar),'0.000')  AS [Consumption Of JW] ,
			format(round(ISNULL(sum([Stock Adjustment Entry]),0),@qtyPar),'0.000')  AS [Stock Adjustment Entry] ,
			format(round(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Net Production Own],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0),@qtyPar),'0.000')  as [Net Consumption Own],

			format(round(
			ISNULL(sum([Op.Factory Owned]),0)+
			ISNULL(sum([Branch Tfr Recd]),0) +
			ISNULL(sum([Purchase]),0) + 
			ISNULL(sum([Warehouse Inwards]),0)  +
			ISNULL(sum([Recd from JW (Own)]),0) -		 
			ISNULL(sum([Sales]),0)-
			ISNULL(sum([Br Transfer Sent]),0) -
			ISNULL(sum([Sent for JW (Own)]),0) +
			(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0)) -
			(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0)-ISNULL(sum([Stock Adjustment Entry]),0)),@qtyPar),'0.000')  as [Cl.Factory Owned],

			format(round(ISNULL(sum([Op.Factory JW (Others)]),0) +ISNULL(sum([Recd For JW (Others)]),0)-ISNULL(sum([Return JW(Others)]),0)-ISNULL(sum([Consumption of JW]),0) +
			ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)], 

			format(round(ISNULL(sum([Op.At JW (Own)]),0)-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)-ISNULL(sum([Consumption At JW]),0)  
			+ISNULL(sum([Production at JW]),0),@qtyPar),'0.000') 
			as [Cl.At JW (Own)]

			-- comment by manish on 3rd May 2025
			--format(round(ISNULL(sum([Op.At JW (Own)]),0)-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)-ISNULL(sum([Consumption At JW]),0) +  
			--ISNULL(sum([Consumption At JW]),0) ,@qtyPar),'0.000')  as [Cl.At JW (Own)]  
		    -- end comment

			from @temp A 
			pivot
			(
			sum(Qty) for Type in ([Op.Factory Owned],[Op.Factory JW (Others)],[Op.At JW (Own)],[Purchase],[Warehouse Inwards],[Branch Tfr Recd],[Recd from JW (Own)],[Recd For JW (Others)],[Sales],
			[Br Transfer Sent],[Sent for JW (Own)],[Return JW(Others)],[Total Production Own+JW],[Total Consumption Own+JW],
			[Production at JW],[Consumption At JW] ,[Pord. of JW],[Consumption of JW],[Stock Adjustment Entry])
			) A
			where a.MainGroup in ( 'RM Consumables')
			group by GROUPING SETS ((A.CompanyName,Sort, a.MainGroup,a.SubGroupName,A.ItemName,a.itemcode),())
			end
			if @ParaRptType=2
			begin
			select case when A.CompanyName is null then 'Total ' else A.CompanyName end CompanyName ,  sysdate as [Date],
			format(round(ISNULL(sum([Op.Factory Owned]),0),@qtyPar),'0.000') as [Op.Factory Owned],
			format(round(ISNULL(sum([Op.Factory JW (Others)]),0),@qtyPar),'0.000') as [Op.Factory JW (Others)],
			format(round(ISNULL(sum([Op.At JW (Own)]),0),@qtyPar),'0.000') as [Op.At JW (Own)],
			format(round(ISNULL(sum([Branch Tfr Recd]),0),@qtyPar),'0.000') AS [Branch Tfr Recd],
			format(round(ISNULL(sum([Purchase]),0) ,@qtyPar),'0.000')  AS [Purchase],
			format(round(ISNULL(sum([Warehouse Inwards]),0) ,@qtyPar),'0.000')  AS [Warehouse Inwards],
			format(round(ISNULL(sum([Recd from JW (Own)]),0),@qtyPar),'0.000')  AS [Recd from JW (Own)],
			format(round(ISNULL(sum([Recd For JW (Others)]),0),@qtyPar),'0.000')  AS [Recd For JW (Others)],
			format(round(ISNULL(sum([Sales]),0),@qtyPar),'0.000')  AS [Sales],
			format(round(ISNULL(sum([Br Transfer Sent]),0),@qtyPar),'0.000')  AS [Br Transfer Sent],
			format(round(ISNULL(sum([Sent for JW (Own)]),0),@qtyPar),'0.000')  AS [Sent for JW (Own)],
			format(round(ISNULL(sum([Return JW(Others)]),0),@qtyPar),'0.000')  AS [Return JW(Others)],
			format(round(ISNULL(sum([Total Production Own+JW]),0),@qtyPar),'0.000')  AS [Total Production Own+JW],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0),@qtyPar),'0.000')  AS [Total Consumption Own+JW],
			format(round(ISNULL(sum([Production at JW]),0),@qtyPar),'0.000')  AS [Production at JW],
			format(round(ISNULL(sum([Consumption At JW]),0),@qtyPar),'0.000')  AS [Consumption At JW] ,
			format(round(ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  AS [Production Of JW],
			format(round(ISNULL(sum([Consumption of JW]),0),@qtyPar),'0.000')  AS [Consumption Of JW] ,
			format(round(ISNULL(sum([Stock Adjustment Entry]),0),@qtyPar),'0.000')  AS [Stock Adjustment Entry] ,
			format(round(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Net Production Own],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0),@qtyPar),'0.000')  as [Net Consumption Own],
			format(round(
			ISNULL(sum([Op.Factory Owned]),0)+
			ISNULL(sum([Branch Tfr Recd]),0) +
			ISNULL(sum([Purchase]),0) + 
			isnull(sum([Warehouse Inwards]),0) +
			ISNULL(sum([Recd from JW (Own)]),0) -		 
			ISNULL(sum([Sales]),0)-
			ISNULL(sum([Br Transfer Sent]),0) -
			ISNULL(sum([Sent for JW (Own)]),0) +
			(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0)) -
			(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0))
			-ISNULL(sum([Stock Adjustment Entry]),0),@qtyPar),'0.000')  as [Cl.Factory Owned],

			format(round(ISNULL(sum([Op.Factory JW (Others)]),0) +ISNULL(sum([Recd For JW (Others)]),0)-ISNULL(sum([Return JW(Others)]),0)-ISNULL(sum([Consumption of JW]),0) +
			ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)], 
			format(round(ISNULL(sum([Op.At JW (Own)]),0)-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)  ,@qtyPar),'0.000')  as [Cl.At JW (Own)]
			--format(round(0+ISNULL(sum([Branch Tfr Recd]),0) +ISNULL(sum([Purchase]),0) + ISNULL(sum([Recd from JW (Own)]),0) + ISNULL(sum([Recd For JW (Others)]),0) -ISNULL(sum([Sales]),0)-ISNULL(sum([Br Transfer Sent]),0) -ISNULL(sum([Sent for JW (Own)]),0),@qtyPar),'0.000')  as [Cl.Factory Owned],

			--format(round(0+ISNULL(sum([Recd For JW (Others)]),0)-ISNULL(sum([Return JW(Others)]),0)-ISNULL(sum([Consumption of JW]),0) +ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)], 
			--format(round(0-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)-ISNULL(sum([Consumption At JW]),0) +  ISNULL(sum([Consumption At JW]),0) ,@qtyPar),'0.000')  as [Cl.At JW (Own)] 

			from @temp A
 
			pivot
			(

			sum(Qty) for Type in ([Op.Factory Owned],[Op.Factory JW (Others)],[Op.At JW (Own)],[Purchase],[Warehouse Inwards], [Branch Tfr Recd],[Recd from JW (Own)],[Recd For JW (Others)],[Sales],
			[Br Transfer Sent],[Sent for JW (Own)],[Return JW(Others)],[Total Production Own+JW],[Total Consumption Own+JW],
			[Production at JW],[Consumption At JW] ,[Pord. of JW],[Consumption of JW],[Stock Adjustment Entry])
			) A
			where a.MainGroup in ('RM','FG','SF' )
			group by GROUPING SETS ((A.CompanyName,sysdate),())
			union all
			select case when A.CompanyName is null then 'Total ' else A.CompanyName end CompanyName ,  sysdate as [Date],
			format(round(ISNULL(sum([Op.Factory Owned]),0),@qtyPar),'0.000') as [Op.Factory Owned],
			format(round(ISNULL(sum([Op.Factory JW (Others)]),0),@qtyPar),'0.000') as [Op.Factory JW (Others)],
			format(round(ISNULL(sum([Op.At JW (Own)]),0),@qtyPar),'0.000') as [Op.At JW (Own)],
			format(round(ISNULL(sum([Branch Tfr Recd]),0),@qtyPar),'0.000') AS [Branch Tfr Recd],
			format(round(ISNULL(sum([Purchase]),0) ,@qtyPar),'0.000')  AS [Purchase],		 
			format(round(ISNULL(sum([Warehouse Inwards]),0) ,@qtyPar),'0.000')  AS [Warehouse Inwards],
			format(round(ISNULL(sum([Recd from JW (Own)]),0),@qtyPar),'0.000')  AS [Recd from JW (Own)],
			format(round(ISNULL(sum([Recd For JW (Others)]),0),@qtyPar),'0.000')  AS [Recd For JW (Others)],
			format(round(ISNULL(sum([Sales]),0),@qtyPar),'0.000')  AS [Sales],
			format(round(ISNULL(sum([Br Transfer Sent]),0),@qtyPar),'0.000')  AS [Br Transfer Sent],
			format(round(ISNULL(sum([Sent for JW (Own)]),0),@qtyPar),'0.000')  AS [Sent for JW (Own)],
			format(round(ISNULL(sum([Return JW(Others)]),0),@qtyPar),'0.000')  AS [Return JW(Others)],
			format(round(ISNULL(sum([Total Production Own+JW]),0),@qtyPar),'0.000')  AS [Total Production Own+JW],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0),@qtyPar),'0.000')  AS [Total Consumption Own+JW],
			format(round(ISNULL(sum([Production at JW]),0),@qtyPar),'0.000')  AS [Production at JW],
			format(round(ISNULL(sum([Consumption At JW]),0),@qtyPar),'0.000')  AS [Consumption At JW] ,
			format(round(ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  AS [Production Of JW],
			format(round(ISNULL(sum([Consumption of JW]),0),@qtyPar),'0.000')  AS [Consumption Of JW] ,
			format(round(ISNULL(sum([Stock Adjustment Entry]),0),@qtyPar),'0.000')  AS [Stock Adjustment Entry] ,
			format(round(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Net Production Own],
			format(round(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0),@qtyPar),'0.000')  as [Net Consumption Own],

			--format(round(0+ISNULL(sum([Branch Tfr Recd]),0) +ISNULL(sum([Purchase]),0) + ISNULL(sum([Recd from JW (Own)]),0) + ISNULL(sum([Recd For JW (Others)]),0) -ISNULL(sum([Sales]),0)-ISNULL(sum([Br Transfer Sent]),0) -ISNULL(sum([Sent for JW (Own)]),0),@qtyPar),'0.000')  as [Cl.Factory Owned],
			--format(round(0+ISNULL(sum([Recd For JW (Others)]),0)-ISNULL(sum([Return JW(Others)]),0)-ISNULL(sum([Consumption of JW]),0) +ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)], 
			--format(round(0-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)-ISNULL(sum([Consumption At JW]),0) +  
			--ISNULL(sum([Consumption At JW]),0) ,@qtyPar),'0.000')  as [Cl.At JW (Own)] 

			format(round(
			ISNULL(sum([Op.Factory Owned]),0)+
			ISNULL(sum([Branch Tfr Recd]),0) +
			ISNULL(sum([Purchase]),0) + 
			ISNULL(sum([Warehouse Inwards]),0) + 
			ISNULL(sum([Recd from JW (Own)]),0) -		 
			ISNULL(sum([Sales]),0)-
			ISNULL(sum([Br Transfer Sent]),0) -
			ISNULL(sum([Sent for JW (Own)]),0) +
			(ISNULL(sum([Total Production Own+JW]),0) -  ISNULL(sum([Pord. of JW]),0)) -
			(ISNULL(sum([Total Consumption Own+JW]),0) -  ISNULL(sum([Consumption Of JW]),0)) -ISNULL(sum([Stock Adjustment Entry]),0) ,@qtyPar),'0.000')  as [Cl.Factory Owned],


			format(round(ISNULL(sum([Op.Factory JW (Others)]),0) +ISNULL(sum([Recd For JW (Others)]),0)-ISNULL(sum([Return JW(Others)]),0)-ISNULL(sum([Consumption of JW]),0) +
			ISNULL(sum([Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)], 

			-- COMMENT BY MANISH ON 3RD MAY 2025
			--format(round(ISNULL(sum([Op.At JW (Own)]),0)-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)-ISNULL(sum([Consumption At JW]),0) +  
			--ISNULL(sum([Consumption At JW]),0) ,@qtyPar),'0.000')  as [Cl.At JW (Own)]
			-- END COMMENT

			format(round(ISNULL(sum([Op.At JW (Own)]),0)-ISNULL(sum([Recd from JW (Own)]),0)+ ISNULL(sum([Sent for JW (Own)]),0)-ISNULL(sum([Consumption At JW]),0)  
			+ISNULL(sum([Production at JW]),0),@qtyPar),'0.000') 
			as [Cl.At JW (Own)]

			from @temp A
 
			pivot
			(
			sum(Qty) for Type in ([Op.Factory Owned],[Op.Factory JW (Others)],[Op.At JW (Own)],[Purchase],[Warehouse Inwards],[Branch Tfr Recd],[Recd from JW (Own)],[Recd For JW (Others)],[Sales],
			[Br Transfer Sent],[Sent for JW (Own)],[Return JW(Others)],[Total Production Own+JW],[Total Consumption Own+JW],
			[Production at JW],[Consumption At JW] ,[Pord. of JW],[Consumption of JW],[Stock Adjustment Entry])
			) A
			where a.MainGroup in ('RM Consumables')
			group by GROUPING SETS ((A.CompanyName,sysdate),())
			end
		end
		 
		 

	 
 
 
END






