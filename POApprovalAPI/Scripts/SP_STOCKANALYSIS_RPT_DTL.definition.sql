CREATE PROCEDURE [dbo].[SP_STOCKANALYSIS_RPT_DTL](
@companyname StringArray READONLY, 
@DATEFROM DATETIME,
@DATETO DATETIME
) WITH RECOMPILE
AS
BEGIN

declare @qtyPar int  =3--(select value from Loginentry.dbo.erp_setting where name='qty') 

 select case when A.CompanyName is null then 'Total ' else A.CompanyName end CompanyName , A.MainGroup,A.SubGroupName,a.ItemName,
 0 as [Op.Factory Owned],
 0 as [Op.Factory JW (Others)],
 0 as [Op.At JW (Own)],
 format(round(ISNULL(sum([1_Branch Tfr Recd]),0),@qtyPar),'0.000') AS [Branch Tfr Recd],
 format(round(ISNULL(sum([2_Purchase]),0) ,@qtyPar),'0.000')  AS [Purchase],
 format(round(ISNULL(sum([3_Recd from JW (Own)]),0),@qtyPar),'0.000')  AS [Recd from JW (Own)],
 format(round(ISNULL(sum([4_Recd For JW (Others)]),0),@qtyPar),'0.000')  AS [Recd For JW (Others)],
 format(round(ISNULL(sum([5_Sales]),0),@qtyPar),'0.000')  AS [Sales],
 format(round(ISNULL(sum([6_Br Transfer Sent]),0),@qtyPar),'0.000')  AS [Br Transfer Sent],
 format(round(ISNULL(sum([7_Sent for JW (Own)]),0),@qtyPar),'0.000')  AS [Sent for JW (Own)],
 format(round(ISNULL(sum([8_Return JW(Others)]),0),@qtyPar),'0.000')  AS [Return JW(Others)],
 format(round(ISNULL(sum([8_Total Production Own+JW]),0),@qtyPar),'0.000')  AS [Total Production Own+JW],
 format(round(ISNULL(sum([9_Total Consumption Own+JW]),0),@qtyPar),'0.000')  AS [Total Consumption Own+JW],
 format(round(ISNULL(sum([10_Production at JW]),0),@qtyPar),'0.000')  AS [Production at JW],
 format(round(ISNULL(sum([11_Consumption At JW]),0),@qtyPar),'0.000')  AS [Consumption At JW] ,
 format(round(ISNULL(sum([12_Pord. of JW]),0),@qtyPar),'0.000')  AS [Production Of JW],
 format(round(ISNULL(sum([13_Consumption of JW]),0),@qtyPar),'0.000')  AS [Consumption Of JW] ,

 format(round(ISNULL(sum([8_Total Production Own+JW]),0) -  ISNULL(sum([10_Production at JW]),0),@qtyPar),'0.000')  as [Net Production Own],
 format(round(ISNULL(sum([9_Total Consumption Own+JW]),0) -  ISNULL(sum([11_Consumption At JW]),0),@qtyPar),'0.000')  as [Net Consumption Own],
 format(round(0+ISNULL(sum([1_Branch Tfr Recd]),0) +ISNULL(sum([2_Purchase]),0) + ISNULL(sum([3_Recd from JW (Own)]),0) + ISNULL(sum([4_Recd For JW (Others)]),0) -ISNULL(sum([5_Sales]),0)-ISNULL(sum([6_Br Transfer Sent]),0) -ISNULL(sum([7_Sent for JW (Own)]),0),@qtyPar),'0.000')  as [Cl.Factory Owned],

 format(round(0+ISNULL(sum([4_Recd For JW (Others)]),0)-ISNULL(sum([8_Return JW(Others)]),0)-ISNULL(sum([13_Consumption of JW]),0) +ISNULL(sum([12_Pord. of JW]),0),@qtyPar),'0.000')  as [Cl.Factory JW (Others)], 
 format(round(0-ISNULL(sum([3_Recd from JW (Own)]),0)+ ISNULL(sum([7_Sent for JW (Own)]),0)-ISNULL(sum([11_Consumption At JW]),0) +  ISNULL(sum([11_Consumption At JW]),0) ,@qtyPar),'0.000')  as [Cl.At JW (Own)] 

 from (
select 
companyname,sysdate,MainGroup,A.GroupName,
case when isnull(SubGroupName,'')='' then A.GroupName else isnull(SubGroupName,'') end SubGroupName, Qty ,Type ,ItemName
from (
select companyname,sysdate,sum(Qty) as Qty,MainGroup,GroupName,Type,SubGroupName,ItemName
from 
(
select 	companyname,sysdate,MRNo,SRNO,
isnull((acceptedqty),0) as qty,
Vw_StoreInwards.itemDeptt as MainGroup,Vw_StoreInwards.GroupName , 
case when  FirmGSTIn= VendorGST then '1_Branch Tfr Recd'  	
when Categoryseries ='JBIN-SE' then '3_Recd from JW (Own)' 	when Categoryseries ='JBIN-OT' then '4_Recd For JW (Others)' 
	else   '2_Purchase'   end as Type ,SubGroupName ,ItemName
from Vw_StoreInwards  with(nolock)  
where     Category not in ('JOB IN') and   Vw_StoreInwards.Cancel !='Cancelled'
) a  group by  CompanyName,SysDate, MainGroup,GroupName,Type,SubGroupName,ItemName

union all

select CompanyName ,SysDate, isnull(sum(acceptedqty),0) qty,
Vw_StoreInwards.itemDeptt as MainGroup,Vw_StoreInwards.GroupName , 
case when  FirmGSTIn= VendorGST then '1_Branch Tfr Recd'  	
	when Categoryseries ='JBIN-SE' then '3_Recd from JW (Own)' 	
	when Categoryseries ='JBIN-OT' then '4_Recd For JW (Others)' 
	else   '2_Purchase'   end as Type ,SubGroupName,ItemName

from Vw_StoreInwards  with(nolock) where   
Category   in ('JOB IN') and Cancel !='Cancelled'
group by SysDate,CompanyName,GroupName ,itemDeptt ,VendorGST,Categoryseries,FirmGSTIn,SubGroupName,ItemName

--union all
--select v.CompanyName ,InwardDate,sum(qty),i.Deptt as  MaingroupName,i.GroupName,'' as VendorGST
--from WareHouseInwards V inner join
--warehouse W on W.ItemCode=V.ItemCode and w.CompanyName=v.CompanyName and w.WareHouseName=v.ToWareHouse
--inner join item i on W.ItemCode=i.ItemCode and w.CompanyName=i.CompanyName   and 
--i.ItemCode=V.ItemCode and i.CompanyName=v.CompanyName  
--where v.transid =0 
--GROUP BY i.GroupName,v.InwardDate, v.CompanyName,i.Deptt

union all

select PurchaseVoucherItem.CompanyName ,PurchaseVoucher.SysDate,sum(ActualQty) as ActualQty,
item.Deptt as  MainGroupName,item.GroupName , 
case when F.NewGSTNo=L.NewGSTNo then  '1_Branch Tfr Recd'  
	when PurchaseVoucher.VoucherType='Job Invoice' then '3_Recd from JW (Own)' else '2_Purchase' end ,item.SubGroupName ,ITEM.ItemName

from PurchaseVoucherItem inner join item on item.CompanyName=PurchaseVoucherItem.CompanyName and 
item.ItemCode=PurchaseVoucherItem.ItemCode inner join PurchaseVoucher on 
PurchaseVoucher.StoreInwardNo=PurchaseVoucherItem.StoreInwardNo and 
PurchaseVoucherItem.CompanyName=PurchaseVoucher.CompanyName 
inner join ledgermaster L On L.CompanyName=PurchaseVoucherItem.CompanyName and 
l.LedgerName=PurchaseVoucher.SupplierName
inner join FactoryInfo f on F.Name=PurchaseVoucher.CompanyName and f.SrNo=PurchaseVoucher.companyId
where PurchaseVoucher.StoreInwardNo in (
select distinct p.StoreInwardNo from PurchaseVoucherItem p inner join PurchaseVoucher V on p.StoreInwardNo=v.StoreInwardNo and 
p.CompanyName=v.CompanyName 
except
select 	distinct SrNo
from Vw_StoreInwards  with(nolock)  where    Vw_StoreInwards.Cancel !='Cancelled' --and Category not in ('JOB IN')
) group by PurchaseVoucherItem.CompanyName ,PurchaseVoucher.SysDate,item.Deptt,item.GroupName,l.NewGSTNo,VoucherType,
F.NewGSTNo,item.SubGroupName,ITEM.ItemName

union all
select I.companyname,S.InvDate AS DespatchDate, SUM(ActualQty) AS qTY ,C.Deptt,C.GroupName, 
case when F.NewGSTNo=L.NewGSTNo then  '6_Br Transfer Sent' else '5_Sales' end Type,C.SubGroupName,C.ItemName

from 
SalesVoucher S with(nolock) inner join SalesVoucherItem I with(nolock) on 
S.companyId=I.companyId and S.CompanyName=I.CompanyName and S.InvNo=I.InvNo and s.InvDate=i.InvDate and s.InvYear=i.Invyear
inner join LedgerMaster L On L.CompanyName=S.CompanyName and L.LedgerName=S.BuyerName
left join Item C with(nolock) on C.CompanyName=I.CompanyName and C.itemcode=I.itemcode
inner join FactoryInfo F on f.name=s.CompanyName and f.SrNo=S.companyId 
WHERE S.VoucherType <>'Job Invoice'
GROUP BY  I.companyname,s.InvDate,I.Commodity,s.InvNo,C.Deptt,C.GroupName,l.NewGSTNo,S.VoucherType,F.NewGSTNo,C.SubGroupName,C.ItemName

union all
select vw_challan5a.companyname ,sysdate,sum(ItemQty)'Qty',item.Deptt,item.GroupName,
case when vw_challan5a.NewGSTNo=[factoryGSTNo] then '6_Br Transfer Sent' else '7_Sent for JW (Own)' end Type ,item.SubGroupName,ITEM.ITEMNAME
from Despatch.dbo.vw_challan5a  with(nolock) inner join Item   with(nolock) on  item.ItemCode= vw_challan5a.ItemCode and 
Item.companyname = vw_challan5a.companyname   
where  (iscancel is null or iscancel = '') 
group by vw_challan5a.companyname ,sysdate,  item.Deptt,item.GroupName,vw_challan5a.NewGSTNo,[factoryGSTNo],item.SubGroupName,ITEM.ITEMNAME

union all
select f.ProcessorName, f.Date,sum(Qty) qty , i.Deptt,i.GroupName,'8_Return JW(Others)',i.SubGroupName,I.ItemName
from Despatch.dbo.vw_SubChallanListMulti f 
inner join MaterialProcessing.dbo.item i 
on i.companyname=f.ProcessorName and i.ItemCode=f.Itemcode
group by f.Date, i.GroupName, f.ProcessorName,i.Deptt,i.GroupName,i.SubGroupName,I.ItemName

UNION ALL

SELECT  companyname,sysdate,SUM(qty) QTY,Deptt,GroupName,'8_Total Production Own+JW' ,SubGroupName,ITEMNAME
FROM [VW_PRODUCTION_STK]
GROUP BY companyname,sysdate,Deptt,GroupName,SubGroupName,ITEMNAME

UNION ALL
SELECT  companyname,sysdate,SUM(qty) QTY,Deptt,GroupName,'9_Total Consumption Own+JW',SubGroupName,VW_Consumption_STK.itemname
FROM VW_Consumption_STK
GROUP BY companyname,sysdate,Deptt,GroupName,SubGroupName,VW_Consumption_STK.itemname

UNION ALL

Select  
a.companyname,A.sysdate,SUM(A.AcceptedQty) QTY,I.Deptt,I.GroupName ,'10_Production at JW',i.SubGroupName,i.ItemName
from Challan5AInward a inner join item i on i.ItemCode=a.Itemcode and i.CompanyName=a.companyname
GROUP BY a.companyname,A.sysdate,I.Deptt,I.GroupName,i.SubGroupName,i.ItemName

UNION ALL

Select Challan5AInward.companyname,Challan5AInward.sysdate,SUM(Challan5AInward.AcceptedQty) QTY,ITEM.Deptt,ITEM.GroupName ,'11_Consumption At JW',
item.SubGroupName,item.ItemName
from Challan5AInward INNER JOIN Despatch..ChallanItem on Challan5AInward.ChallanSubCode=ChallanItem.Code 
OR Challan5AInward.CombineChallanNo=ChallanItem.Code 
INNER JOIN Despatch..Challan5A on ChallanItem.Code=Challan5A.ChallanNo 
INNER JOIN Despatch.dbo.CompanyMaster ON dbo.Challan5AInward.BuyerName = Despatch.dbo.CompanyMaster.CompanyName INNER JOIN
dbo.FactoryInfo ON Challan5AInward.companyname = dbo.FactoryInfo.Name
INNER JOIN ITEM ON ITEM.ItemCode=ChallanItem.ItemCode AND ITEM.CompanyName=Challan5AInward.companyname
GROUP BY Challan5AInward.companyname,Challan5AInward.sysdate,ITEM.Deptt,ITEM.GroupName, item.SubGroupName,item.ItemName

UNION ALL

select  V.ProcessorName ,V.DATE,SUM(v.Qty) AS QTY,  i.Deptt,i.GroupName,'12_Pord. of JW',i.SubGroupName,i.ItemName
from Despatch.DBO.vw_SubChallanListMulti v 
inner join MaterialProcessing.dbo.item  i on i.itemcode=v.Itemcode and i.CompanyName=v.ProcessorName
inner join Despatch.DBO.vw_subsidiaryChallanItem O on O.ProcessorName=V.ProcessorName and O.MainChallanDate=v.MainChallanDate and O.MainChallanNo=v.MainChallanNo
inner join MaterialProcessing.dbo.item i1 on i1.itemcode =o.itemcode and i1.CompanyName=O.ProcessorName
GROUP BY V.ProcessorName ,V.DATE,i.Deptt,i.GroupName,i.SubGroupName,i.ItemName

 UNION ALL

select  V.ProcessorName ,V.DATE,SUM(v.Qty) AS QTY,  i1.Deptt,i1.GroupName,'13_Consumption of JW',i1.SubGroupName,i1.ItemName
from Despatch.DBO.vw_SubChallanListMulti v 
inner join MaterialProcessing.dbo.item  i on i.itemcode=v.Itemcode and i.CompanyName=v.ProcessorName
inner join Despatch.DBO.vw_subsidiaryChallanItem O on O.ProcessorName=V.ProcessorName and O.MainChallanDate=v.MainChallanDate and O.MainChallanNo=v.MainChallanNo
inner join MaterialProcessing.dbo.item i1 on i1.itemcode =o.itemcode and i1.CompanyName=O.ProcessorName
GROUP BY V.ProcessorName ,V.DATE,i1.Deptt,i1.GroupName,i1.SubGroupName,i1.ItemName



) A 
inner join FactoryInfo F on F.Name=A.CompanyName
inner join @companyname t on CompanyName = t.StringValue
where   

 

A.SysDate BETWEEN @DATEFROM AND @DATETO and A.Qty!=0
) A
pivot
(

sum(Qty) for Type in ([2_Purchase],[1_Branch Tfr Recd],[3_Recd from JW (Own)],[4_Recd For JW (Others)],[5_Sales],
[6_Br Transfer Sent],[7_Sent for JW (Own)],[8_Return JW(Others)],[8_Total Production Own+JW],[9_Total Consumption Own+JW],
[10_Production at JW],[11_Consumption At JW] ,[12_Pord. of JW],[13_Consumption of JW])
) A
where a.MainGroup in ('RM','FG','SF', 'RM Consumables')
group by GROUPING SETS ((A.CompanyName, a.MainGroup,a.SubGroupName,a.ItemName),())
--order by A.CompanyName,A.MainGroup,A.SubGroupName
END
