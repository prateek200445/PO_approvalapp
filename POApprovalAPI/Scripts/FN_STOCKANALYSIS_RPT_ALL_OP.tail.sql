S DATE) >=@PARADATEFROM 
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






