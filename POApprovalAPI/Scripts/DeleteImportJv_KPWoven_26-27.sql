-- Delete Import JV journals posted to K.P. WOVEN PRIVATE LIMITED (yr 26-27)
-- from the portal as gstho1. Review the SELECT results before the DELETE.
-- Ran 2026-11-19: 232 Journal lines + 232 JournalItem lines backed up then deleted; 0 remaining.
-- Ran 2026-11-19 (remaining 3053, 3054, 3107, 3109, 3132): 12 Journal lines backed up then deleted; 0 remaining.
--
-- Do not delete a contiguous JournalNo range — other vouchers may sit in between.
-- AccountBills rows are left in place unless you confirm they belong only to this import.

DECLARE @Company varchar(200) = N'K.P. WOVEN PRIVATE LIMITED'; -- confirm FactoryInfo.Name
DECLARE @Yr varchar(10) = N'26-27';
DECLARE @User varchar(50) = N'gstho1'; -- portal user on these rows (not gslho1)

IF OBJECT_ID('tempdb..#Jv') IS NOT NULL DROP TABLE #Jv;
CREATE TABLE #Jv (JournalNo int NOT NULL PRIMARY KEY);

INSERT INTO #Jv (JournalNo) VALUES
 (3053),(3054),
 (3057),(3058),(3059),(3060),
 (3061),(3062),(3063),(3064),(3065),(3066),(3067),(3068),(3069),(3070),
 (3071),(3072),(3073),(3074),(3075),(3076),(3077),(3078),(3079),(3080),
 (3081),(3082),(3083),(3084),(3085),(3086),(3087),
 (3088),(3089),(3090),(3091),(3092),(3093),(3094),(3095),(3096),(3097),
 (3098),(3099),(3100),(3101),(3102),(3103),
 (3104),(3105),(3106),(3107),
 (3109),(3110),(3111),
 (3112),(3113),(3114),(3115),(3116),(3117),(3118),(3119),(3120),(3121),
 (3122),(3123),(3124),(3125),(3126),(3127),(3128),(3129),(3130),(3131),(3132),
 (3133),(3134);

-- 1. Inspect. Stop here if anything is not this import.
SELECT j.JournalNo, j.sysdate, j.Type, j.PartyName, j.Debit, j.Credit,
       j.UserName, j.MRNo, j.InwardNo, j.narrationFinal, j.yr
FROM Journal j WITH (NOLOCK)
INNER JOIN #Jv n ON n.JournalNo = j.JournalNo
WHERE LTRIM(RTRIM(j.CompanyName)) = @Company
  AND j.yr = @Yr
ORDER BY j.JournalNo, j.Type, j.PartyName;

SELECT DISTINCT j.JournalNo, j.UserName, j.sysdate
FROM Journal j WITH (NOLOCK)
INNER JOIN #Jv n ON n.JournalNo = j.JournalNo
WHERE LTRIM(RTRIM(j.CompanyName)) = @Company
  AND j.yr = @Yr
  AND LTRIM(RTRIM(ISNULL(j.UserName, ''))) <> @User; -- expect 0 after using gstho1

-- 2. After the SELECT looks right, run this batch in a transaction.
BEGIN TRANSACTION;

INSERT INTO journal_delete
SELECT j.*
FROM Journal j
INNER JOIN #Jv n ON n.JournalNo = j.JournalNo
WHERE LTRIM(RTRIM(j.CompanyName)) = @Company
  AND j.yr = @Yr
  AND LTRIM(RTRIM(ISNULL(j.UserName, ''))) = @User;

INSERT INTO Journalitem_delete
SELECT i.*
FROM JournalItem i
INNER JOIN #Jv n ON n.JournalNo = i.JournalNo
WHERE LTRIM(RTRIM(i.CompanyName)) = @Company
  AND i.yr = @Yr
  AND LTRIM(RTRIM(ISNULL(i.UserName, ''))) = @User;

DELETE cce
FROM CostCenterEntries cce
INNER JOIN Journal j ON cce.srno = j.transid
INNER JOIN #Jv n ON n.JournalNo = j.JournalNo
WHERE cce.VoucherType = 'Journal'
  AND LTRIM(RTRIM(j.CompanyName)) = @Company
  AND j.yr = @Yr
  AND LTRIM(RTRIM(ISNULL(j.UserName, ''))) = @User;

DELETE inv
FROM JournalInventory inv
INNER JOIN #Jv n ON n.JournalNo = inv.JournalNo
WHERE LTRIM(RTRIM(inv.CompanyName)) = @Company
  AND inv.yr = @Yr;

DELETE i
FROM JournalItem i
INNER JOIN #Jv n ON n.JournalNo = i.JournalNo
WHERE LTRIM(RTRIM(i.CompanyName)) = @Company
  AND i.yr = @Yr
  AND LTRIM(RTRIM(ISNULL(i.UserName, ''))) = @User;

DELETE j
FROM Journal j
INNER JOIN #Jv n ON n.JournalNo = j.JournalNo
WHERE LTRIM(RTRIM(j.CompanyName)) = @Company
  AND j.yr = @Yr
  AND LTRIM(RTRIM(ISNULL(j.UserName, ''))) = @User;

-- COMMIT;  -- uncomment after row counts look right
ROLLBACK;
