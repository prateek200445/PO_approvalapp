# Stock workflow bugs (definite)

**Date:** 16 Nov 2026  
**Scope:** Bugs that still run after opening / warehouse is reset to correct values.  
**Out of scope:** Stale openings; Despatch-no-plus and FIBC item-code change (intended); tick does not affect Stock Analysis.

| ID | Area | Bug | After a clean reset |
|----|------|-----|---------------------|
| **R1** | Roll Transfer (warehouse) | Despatch minus uses grid row `i`, not the ticked roll | Yes — wrong item if ticks are not the first N rows |
| **S1** | Stock Analysis SP | `stockjv` has no from-date | Yes — old JVs enter this period again |
| **S2** | Stock Analysis SP | Pre-sales has no report from/to date | Yes — extra Sales possible |
| **S3** | Stock Analysis SP | RM Consumables closing uses opposite sign on Stock Adjustment | Yes when a Consumables item is adjusted (0 kg in FY 25-26) |

**Objects**

- Roll: `PO_approvalapp/FrmWareHouseTransferNew.cs`
- Report copy: `PO_approvalapp/POApprovalAPI/Scripts/SP_STOCKANALYSIS_RPT_ALL.definition.sql`
- Live SP: `dbo.SP_STOCKANALYSIS_RPT_ALL` on `MaterialProcessing`

The `.sql` file in this repo is a copy. Changing it does not update the live database until the SP is deployed.

---

## R1 — Roll Transfer Despatch: warehouse minus hits the wrong item

### Plain language

On **Roll Transfer** to **Despatch Godown**, ticked rolls are copied into `myDespatch` (compact list: row 0 = first tick). Warehouse minus then uses **`dataGridView1` row `i`**, which is the **Nth line on the screen**, including unticked rows.

- Journal / packing list = ticked roll’s `FGITEMCODE` (**correct**).
- Warehouse `StkInHand` = grid row `i` (**wrong** if you did not Select All from the top).

**Select All:** row `i` matches; no damage.  
**Tick 3 rolls in the middle of 200:** warehouse minuses the **first 3 visible rows**.

This does **not** change Stock Analysis Own (that report does not use `WareHouse.StkInHand`; Despatch journals are not in `vw_consumption_stk_FG`).

### Example

| Screen row | Tick | Item | kg |
|------------|------|------|----|
| 0 | no | WIP00023 | 40 |
| 1 | no | WIP00023 | 50 |
| 2 | yes | WIP00025 | 62 |
| 3 | yes | WIP00025 | 60 |

`myDespatch` has 2 rows (both WIP00025, 62 + 60). Loop `i = 0, 1` minuses **WIP00023** 62 then 50 — not the ticked fabric.

Warehouse: WIP00023 too low, WIP00025 too high vs remaining rolls.

### Current code

Ticks only go into `myDespatch` (`FrmWareHouseTransferNew.cs` ~450–473):

```csharp
for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
{
    if (dataGridView1.Rows[i].Cells[0].FormattedValue.ToString() == "True")
    {
        myDespatch.Rows.Add(/* ... */, Convert.ToString(dataGridView1["fgitemcode", i].Value));
    }
}
```

Minus uses **grid index** (~572–579):

```csharp
for (int i = 0; i <= myDespatch.Rows.Count - 1; i++)
{
    string ITEMCODE = Convert.ToString(myDespatch.Rows[i]["FGITEMCODE"]);
    double Netwt = Utility.SafeConvertToDouble(myDespatch.Rows[i]["NetWt"]);
    ProductionCommonFunction.InsertStockJournal(..., ITEMCODE, rollno, "Despatch");
    Database.GetExecuteNonQueryCommand(
        "Update Warehouse set stkinHand=case when Round(stkinHand - " + Netwt +
        ",2)<0 then 0 else Round(stkinHand - " + Netwt +
        ",2) end where CompanyName='" + comboCompanyName.Text +
        "' and WarehouseName='" + comboWareHouseFrom.Text +
        "' and ItemCode='" + Convert.ToString(dataGridView1["fgitemcode", i].Value) + "'");
}
```

`ITEMCODE` is already the ticked item. The `UPDATE` must use it, not `dataGridView1["fgitemcode", i]`.

### Corrected code

```csharp
for (int i = 0; i <= myDespatch.Rows.Count - 1; i++)
{
    string ITEMCODE = Convert.ToString(myDespatch.Rows[i]["FGITEMCODE"]);
    string ITEMNAME = Convert.ToString(Database.GetExecuteNonQueryCommand_retrun(
        "select distinct top 1 itemname from itemcodes with(nolock) where itemcode='" + ITEMCODE + "'"));
    string rollno = Convert.ToString(myDespatch.Rows[i]["RollNO"]);
    double Netwt = Utility.SafeConvertToDouble(myDespatch.Rows[i]["NetWt"]);
    ProductionCommonFunction.InsertStockJournal(0, dateTimePicker1.Value, comboCompanyName.Text,
        "Consumption", ITEMNAME, Netwt, 0, 0, "KGS", frmDefaultVale.Year, 0, 0, ITEMCODE, rollno, "Despatch");
    Database.GetExecuteNonQueryCommand(
        "Update Warehouse set stkinHand=case when Round(stkinHand - " + Netwt +
        ",2)<0 then 0 else Round(stkinHand - " + Netwt +
        ",2) end where CompanyName='" + comboCompanyName.Text +
        "' and WarehouseName='" + comboWareHouseFrom.Text +
        "' and ItemCode='" + ITEMCODE.Replace("'", "''") + "'");
}
```

The clamp (`<0 then 0`) is unchanged. After a correct warehouse reset and this item fix, clamp should not fire on a normal Despatch.

---

## S1 — Stock Analysis: `stockjv` has no from-date

### Plain language

Period columns (Purchase, Sales, production, …) use `between @DATEFROM and @DATEto`.  
`stockjv` is only `sysdate <= @DATEto`.

Old JVs (already inside a reset opening) are **added again** as production/consumption in this year.

### Example

FY 25-26, opening reset at 31 Mar 2025. Table still has 31 May 2024 JV: HCP `RAW05974`, type `Total Consumption Own+JW`, **13,450 kg**.

`13,450` is `<= 31 Mar 2026` → period consumption **and** opening. Own is reduced twice.

**Live (FY 25-26):** 103 rows before 1 Apr 2025, **~8.32 lakh kg**. Types include `Total Consumption Own+JW`, `Total Production Own+JW`, JW prod/cons. In-year JVs: 2 rows, qty 0.

Resetting opening does **not** delete `stockjv`. The leak remains until the `WHERE` is fixed.

### Current code

`SP_STOCKANALYSIS_RPT_ALL.definition.sql` ~304–309:

```sql
--- for internal stock JV added by manish on 13th Sep 2023
select stockjv.companyname,sysdate,qty,item.deptt,item.groupname,type,
       item.subgroupname,item.itemname,srno,stockjv.itemcode,challanno
from stockjv
inner join @companyname t on stockjv.companyname = t.StringValue
inner join item on item.ItemCode = StockJV.itemcode and item.CompanyName = StockJV.companyname
where sysdate <= @DATEto
```

Opening function `FN_STOCKANALYSIS_RPT_ALL_OP` already uses a range (`sysdate between @openingdate and @Paraopdate`). The **main SP period** is the leak.

### Corrected code

```sql
select stockjv.companyname,sysdate,qty,item.deptt,item.groupname,type,
       item.subgroupname,item.itemname,srno,stockjv.itemcode,challanno
from stockjv
inner join @companyname t on stockjv.companyname = t.StringValue
inner join item on item.ItemCode = StockJV.itemcode and item.CompanyName = StockJV.companyname
where sysdate >= @DATEFROM and sysdate <= @DATEto
```

---

## S2 — Stock Analysis: pre-sales not limited to the report period

### Plain language

`@openingdate` is `MIN(OpeningBalanceDate)` for **all companies** (live **31 Mar 2021**, Marudhar Polymers). Pre-sales (`vw_SalesRegister` invoices **not** in `SalesVoucher`) uses `DespatchDate >= @openingdate` and **does not** use `@DATEFROM` / `@DATEto`.

Unmatched packing-list / register qty from years ago (or after the To date) can hit **this** run as **Sales**, so Own drops.

### Example

You run one company, Apr 2025–Mar 2026, opening reset. Global min is still 2021. A 2022 register invoice never posted to `SalesVoucher` is still `DespatchDate >= 2021` → counted as FY 25-26 Sales.

If every company’s snapshot date is moved to FY start, the floor moves up; the SP **still** has no period box.

Live kg for this union timed out on `vw_SalesRegister`. The missing date filter is still the bug.

### Current code

~line 32:

```sql
declare @openingdate date = (select min(OpeningBalanceDate) from OpeningBalanceCommoditywise)
```

~253–267:

```sql
--- pre sales data which is not reflected in sales voucher 14th July 2022
select vw_SalesRegister.companyname, vw_SalesRegister.DespatchDate, sum(Netwt), Deptt, i.GroupName,
       'Sales' Type, i.SubGroupName, i.ItemName, 5, vw_SalesRegister.Itemcode, vw_SalesRegister.invno
from vw_SalesRegister with(nolock)
inner join item i with(nolock) on vw_SalesRegister.Itemcode = i.ItemCode
inner join @companyname t on vw_SalesRegister.companyname = t.StringValue
  and vw_SalesRegister.companyname = i.CompanyName
where CONVERT(char(10), DespatchDate, 120) >= @openingdate
  and vw_SalesRegister.Invno not in ( /* SalesVoucher / Journal */ )
group by ...
```

### Corrected code

Keep the “not already in SalesVoucher” check. Bound by the report dates (company is already joined):

```sql
where CAST(vw_SalesRegister.DespatchDate AS date) between @DATEFROM and @DATEto
  and vw_SalesRegister.Invno not in (
        select InvNo from MaterialProcessing..salesvoucher
        where vw_SalesRegister.aresuffix = InvYear
          and vw_SalesRegister.companyname = MaterialProcessing..salesvoucher.companyname
          and InvNo is not null
        union
        select InwardNo from Journal
        where vw_SalesRegister.aresuffix = Journal.yr
          and vw_SalesRegister.companyname = Journal.CompanyName
          and InwardNo is not null
      )
```

Stop using global `MIN(OpeningBalanceDate)` for this union; the period dates are enough.

---

## S3 — Stock Analysis: RM Consumables closing adds Stock Adjustment instead of subtracting

### Plain language

The report prints two blocks (`UNION ALL`):

1. `MainGroup in ('RM','FG','SF')`
2. `MainGroup in ('RM Consumables')`

Same **Stock Adjustment Entry** column. **Cl.Factory Owned** is not the same.

- RM / SF / FG: `… − NetCons − Adj`
- Consumables (`@RptType` 0 and 1): `… − (NetCons − Adj)` = `… − NetCons + Adj`

Write-off 100 kg Consumables → closing **+100** instead of **−100** (Own wrong by **200**).

FY 25-26 Consumables qty to Stock Adjustment = **0** (idle until they post one). Fabric Own / negative Own is not this bug.

`@RptType = 2` (date-wise) Consumables already minuses Adj outside (~850–860). Fix **0** (subgroup) and **1** (item-wise) only.

### Example

Opening 500, only Stock Adjustment 100:

| | RM fabric | RM Consumables (`@RptType` 0/1) |
|--|-----------|----------------------------------|
| Cl.Factory Owned | 400 | 600 |

Same posting (to godown `Stock Adjustment Entry`). Consumables is a fourth `Item.Deptt`, so it has its own `SELECT`. The extra `- Adj` was put **inside** the consumption brackets.

### Current code

RM / SF / FG (`@RptType = 0`, ~529–540, filter ~570):

```sql
(ISNULL(sum([Total Production Own+JW]),0) - ISNULL(sum([Pord. of JW]),0)) -
(ISNULL(sum([Total Consumption Own+JW]),0) - ISNULL(sum([Consumption Of JW]),0)) -
ISNULL(sum([Stock Adjustment Entry]),0)
-- where a.MainGroup in ('RM','FG','SF')
```

RM Consumables (`@RptType = 0`, ~597–607, filter ~634):

```sql
(ISNULL(sum([Total Production Own+JW]),0) - ISNULL(sum([Pord. of JW]),0)) -
(ISNULL(sum([Total Consumption Own+JW]),0) - ISNULL(sum([Consumption Of JW]),0)
 - ISNULL(sum([Stock Adjustment Entry]),0))
-- where a.MainGroup in ('RM Consumables')
```

Same inner-bracket pattern at `@RptType = 1` Consumables ~731–741 (`where a.MainGroup in ('RM Consumables')` ~762).

### Corrected code (Consumables — match RM/SF/FG)

Use this for `@RptType` **0** and **1** Consumables `Cl.Factory Owned`:

```sql
format(round(
ISNULL(sum([Op.Factory Owned]),0)+
ISNULL(sum([Branch Tfr Recd]),0) +
ISNULL(sum([Purchase]),0) +
ISNULL(sum([Warehouse Inwards]),0) +
ISNULL(sum([Recd from JW (Own)]),0) -
ISNULL(sum([Sales]),0)-
ISNULL(sum([Br Transfer Sent]),0) -
ISNULL(sum([Sent for JW (Own)]),0) +
(ISNULL(sum([Total Production Own+JW]),0) - ISNULL(sum([Pord. of JW]),0)) -
(ISNULL(sum([Total Consumption Own+JW]),0) - ISNULL(sum([Consumption Of JW]),0)) -
ISNULL(sum([Stock Adjustment Entry]),0)
,@qtyPar),'0.000') as [Cl.Factory Owned],
```

---

## Not in this list (on purpose)

| Topic | Why not a definite workflow bug here |
|-------|--------------------------------------|
| Despatch: no plus on Despatch Godown | Intended |
| FIBC / Small Bag: plus WIP item, not fabric | Intended |
| Despatch journal as Consumption | Not in `vw_consumption_stk_FG` |
| Roll Transfer `dtFinal` / backdate | Save-day `dModifyDate` still minuses; not a skip |
| Credit notes commented / debit notes on | Finance turned CN off; confirm before treating as a bug |
| Negative qty in production / consumption views | View/source, not these three SP lines |
| Wastage `transid = 0` Warehouse Inwards | Policy (adds to Own); not a date/formula slip |
| Tick vs Stock Analysis | Tick does not feed Own |

---

## Suggested fix order

1. **R1** in `FrmWareHouseTransferNew.cs` (warehouse vs remaining rolls).  
2. **S1** in `SP_STOCKANALYSIS_RPT_ALL` (`stockjv` date). Deploy to live DB.  
3. **S2** pre-sales period dates. Deploy with S1.  
4. **S3** Consumables closing in `@RptType` 0 and 1. Deploy with S1.

Related longer register (openings, performance, wastage policy): `STOCK_ANALYSIS_ISSUES_REPORT.md`.
