using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Models
{
    public sealed class BomCreateRequest
    {
        public BomCreateHeaderInput Header { get; set; } = new();
        public List<BomCreateLineInput> Lines { get; set; } = [];
        public List<string> Approvals { get; set; } = [];
        public Dictionary<string, string?> Bom1Values { get; set; } = new();
        public Dictionary<string, string?> Bom3Values { get; set; } = new();
    }

    public sealed class BomCreateHeaderInput
    {
        public string FilePoNo { get; set; } = "";
        public string Customer { get; set; } = "";
        public DateTime? SysDate { get; set; }
        public string PrintType { get; set; } = "";
        public string PoNo { get; set; } = "";
        public string PoNos { get; set; } = "";
        public string BagType { get; set; } = "";
        public double? SizeL { get; set; }
        public double? SizeW { get; set; }
        public double? SizeH { get; set; }
        public string SizeType { get; set; } = "";
        public string Swl { get; set; } = "";
        public string SfRatio { get; set; } = "";
        public string Qty { get; set; } = "";
        public string QtyUnit { get; set; } = "";
        public string FSType { get; set; } = "";
        public string DSType { get; set; } = "";
        public string DSType1 { get; set; } = "";
        public string DSType2 { get; set; } = "";
        public string LoopType { get; set; } = "";
        public string FabColor { get; set; } = "";
        public string Instruction { get; set; } = "";
        public string BodyRemarks { get; set; } = "";
        public string PrintingRemarks { get; set; } = "";
        public string RefNo { get; set; } = "";
        public string Doc { get; set; } = "";
        public string Doc1 { get; set; } = "";
        public string Doc2 { get; set; } = "";
        public string DocUnit { get; set; } = "";
        public string DocNumber { get; set; } = "";
        public string KnotType { get; set; } = "";
        public string RpFabric { get; set; } = "";
        public bool IsDropLoop { get; set; }
        public string UserName { get; set; } = "";
    }

    public sealed class BomCreateLineInput
    {
        public int SortOrder { get; set; }
        public string Heading { get; set; } = "";
        public string Gsm { get; set; } = "";
        public string Lami { get; set; } = "";
        public string Color { get; set; } = "";
        public string FabricSize { get; set; } = "";
        public string CutSize { get; set; } = "";
        public double? TotalMtr { get; set; }
        public double? TotalKg { get; set; }
        public string Remarks { get; set; } = "";
        public string Gpm { get; set; } = "";
    }

    public sealed class BomCreatePreviewResult
    {
        public string PreviewId { get; set; } = "";
        public string FilePoNo { get; set; } = "";
        public string Customer { get; set; } = "";
        public int LineCount { get; set; }
        public double TotalKg { get; set; }
        public IReadOnlyList<string> Approvals { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
        public IReadOnlyList<BomCreateLineInput> Lines { get; set; } = Array.Empty<BomCreateLineInput>();
    }

    public sealed class BomSaveResult
    {
        public string FilePoNo { get; set; } = "";
        public string SrNo { get; set; } = "";
        public bool Created { get; set; }
        public bool Updated { get; set; }
        public double TotalKg { get; set; }
        public int LineCount { get; set; }
    }

    public sealed class BomEditorSnapshot
    {
        public BomCreateHeaderInput Header { get; set; } = new();
        public IReadOnlyList<string> Approvals { get; set; } = Array.Empty<string>();
        public IReadOnlyList<BomCreateLineInput> Lines { get; set; } = Array.Empty<BomCreateLineInput>();
        public Dictionary<string, string?> Bom1Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string?> Bom3Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

namespace POApprovalAPI.Services
{
    using POApprovalAPI.Models;

    public sealed class BomCreationService
    {
        private const int BomCommandTimeoutSeconds = 120;
        private const string PartyMappingCacheKey = "bom:party-mapping:v2";
        private const string UsersCacheKey = "bom:users:v1";
        private const string PreviewCacheKeyPrefix = "bom:preview-report:";
        private static readonly TimeSpan PreviewCacheTtl = TimeSpan.FromMinutes(20);

        private static readonly HashSet<string> ReservedBom1Columns = new(StringComparer.OrdinalIgnoreCase)
        {
            "SysDate", "Customer", "PrintType", "FilePONo", "BagType", "SizeL", "SizeW", "SizeH", "SizeType",
            "SWL", "S", "Qty", "QtyUnit", "FSType", "DSType", "DSType1", "DSType2", "FabColor", "TotalKg",
            "Instruction", "BodyRemarks1", "SrNo", "pono", "ponos", "printingremarks", "refNo", "UserName",
            "Doc", "Doc1", "Doc2", "DocUnit", "DocNumber", "looptype", "IsDropLoop", "ApprovalField",
            "Knottype", "RPfabric"
        };

        private static readonly HashSet<string> ReservedBom3Columns = new(StringComparer.OrdinalIgnoreCase)
        {
            "SrNo", "PONO", "dModifyDate"
        };

        private static readonly HashSet<string> AllowedBom1ExtraColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "ShortLen", "LoopL", "LoopW", "LoopDim", "L", "LinerL", "LinerW", "LinerDim", "Liner", "LinerType",
            "DSL", "DSW", "DSDim", "FSL", "FSW", "FSDim", "SlitHt", "FillHt", "TotalHt", "SideGSM", "SideLami",
            "BodyLami", "BodyGSM", "BodyFabric", "BodyCutSize", "BodyTotalMtr", "BodyTotalKg", "BodyRemarks",
            "SideFabric", "SideCutSize", "SideTotalMtr", "SideTotalKg", "SideRemarks",
            "TopGSM", "TopLami", "TopFabric", "TopCutSize", "TopTotalMtr", "TopTotalKg", "TopRemarks", "TopRemarks1",
            "BottomGSM", "BottomLami", "BottomFabric", "BottomCutSize", "BottomTotalMtr", "BottomTotalKg", "BottomRemarks",
            "BottomRemarks1", "FSGSM", "FSLami", "FSFabric", "FSCutSize", "FSTotalMtr", "FSTotalKg", "FSRemarks",
            "FSTieGSM", "FSTieLami", "FSTieFabric", "FSTieCutSize", "FSTieTotalMtr", "FSTieTotalKg", "FSTieRemarks",
            "FSTieRemarks1", "DSGSM", "DSLami", "DSFabric", "DSCutSize", "DSTotalMtr", "DSTotalKg", "DSRemarks",
            "DSTieGSM", "DSTieLami", "DSTieFabric", "DSTieCutSize", "DSTieTotalMtr", "DSTieTotalKg", "DSTieRemarks",
            "DSTieRemarks1", "LoopGSM", "LoopColor", "LoopFabric", "LoopCutSize", "LoopTotalMtr", "LoopTotalKg",
            "LoopRemarks", "LoopRemarks1", "LinerGSM", "LinerLami", "LinerFabric", "LinerCutSize", "LinerTotalMtr",
            "LinerTotalKg", "LinerRemarks", "LinerRemarks1", "DocGSM", "DocLami", "DocCutSize", "DocTotalMtr",
            "DocTotalKg", "DocRemarks", "LableGSM", "LableFabric", "LableCutSize", "LableTotalMtr", "LableTotalKg",
            "LabelColor", "LabelRemarks1", "ThreadTotalKg", "ThreadRemarks", "FillerCordGSM", "FillerCordTotalKg",
            "DropLoop", "loopRemarks", "DuffleHt", "LinerCutSize", "LinerFabric", "bodyno", "isrf", "conicaltop",
            "docl", "docw", "loopslitremarks", "DSL1", "DSW1", "DSL2", "DSW2", "bottomno", "sideno", "doc1unit",
            "doc2unit", "docl1", "docw1", "docl2", "docw2", "BuffleType", "loopconst", "BottomSkritH",
            "looplongleg", "slitlenght"
        };

        private static readonly HashSet<string> AllowedBom3Columns = new(StringComparer.OrdinalIgnoreCase)
        {
            "RF", "Thread", "Hiracle", "HiracleTop", "HIracleBottom", "ThreadBuffleSeam", "ThreadNeedle",
            "DropLoop", "TillTheBottom", "LoopLength", "fillercord", "fillercordtop", "fillercordbottom",
            "fillercordtopspout", "fillercordbottomspout", "fillercordbody", "fillercordbuffle", "isfillercord",
            "threadColor", "threadtype", "threaddenier", "feltbody", "lineratpoint", "toptypes", "bottomtypes",
            "felttop", "feltBottom", "felttopspout", "feltBottomspout", "FeltUnderTheLoop", "fsno", "dsno",
            "dsno1", "dsno2", "conicalheight", "StartSewnBaseHt", "Stevdoreno", "topno", "topflapno",
            "bottomflapno", "bottomloopno", "BuffleType", "BuffleSideA", "BuffleSideB", "SubBuffleType",
            "feltMFWeb", "DoubleFoldBody", "DoubleFoldTop", "DoubleFoldBottom", "DoubleFoldBottomSpout",
            "DoubleFoldBottomSpout2", "BoxSpoutConical", "IsTopVelcro", "TopVelcro", "IsTopHoseSlider",
            "TopHoseSlider", "IsCableTie", "CableTie", "bottomgsm1", "Isbottomlam1", "BottomSubTypeLamiGSM",
            "Isbottomtieextra", "Isbottomvelcro", "bottomvelcro", "IsBottomhoseslider", "Bottomhoseslider",
            "IsBoxbottomwiretie", "bottomwiretie", "Isbottomcabletie", "bottomcabletie", "BottomConicalHeight",
            "BottomSubTypeRemarks", "BottomSubTypeColor", "IsBS1bottomtieextra1", "IsBS1bottomvelcro1",
            "BS1bottomvelcro1", "IsBS1Bottomhoseslider1", "BS1Bottomhoseslider1", "IsBS1bottomwiretie1",
            "BS1bottomwiretie1", "IsBS1bottomcabletie1", "BS1bottomcabletie1", "BS1SkirtHeight1", "BS1Bottomrem1",
            "BS1bottomgsm2", "IsBS1bottomlam2", "BS1BottomLamiGSM1", "BS1BottomNo1", "BS1BottomColor1",
            "BS1SpoutRope1", "BS1SpoutRopeSize1", "BS1SpoutRopeNo1", "BS1SpoutRopeColor1", "BS1spoutroperemarks1",
            "BS1TieGrm1", "BS1TieSize1", "BS1TieCutSize1", "BS1SpoutTieNo1", "ISBottomSpoutRope1", "BS2bottomgsm4",
            "IsBS2bottomlam4", "BS2LamiGSM2", "BS2No2", "BS2Color2", "BS2rem2", "IsBS2tieextra2", "IsBS2velcro2",
            "BS2velcro2", "IsBS2hoseslider2", "BS2hoseslider2", "IsBS2wiretie2", "BS2wiretie2", "IsBS2cabletie2",
            "BS2cabletie2", "BS2SkirtHeight2", "BS2SpoutRope2", "BS2SpoutRopeSize2", "BS2SpoutRopeNo2",
            "BS2TieSize2", "BS2TieCutSize2", "BS2TieNo2", "ISlabel", "Isblock", "blocknos",
            "InnerSkinExtraCutLenght", "InnerSkinSize", "InnerTopSize", "InnerBottomSize", "InnerTopExtra",
            "InnerBottomExtra", "InnerTopDia", "InnerBottomDia", "InnerTopHeight", "InnerBottomheight",
            "LoopProtectorSize", "IsMFWebTop", "IsMFWebBottom", "IsMFWebTopSpout", "IsMFWebBottomSpout",
            "IsMFWebBody", "BottomFlapCutLenght", "ISTopFlapDRing", "BottomBoxtopflapdring", "IsBottomFlapDRing",
            "Boxbottomflapdring", "SpoutRopeType", "IsExtraLabel", "ExtraLabelNo", "ExtralabelL", "ExtralabelW",
            "ExtraLabelMicron", "IsBoXExtraLabel", "ExtraLabelLam", "ExtraLabeltype", "Extralabelsubtype",
            "ExtralabelL1", "ExtralabelW1", "ExtraLabelMicron1", "IsBoXExtraLabel1", "ExtraLabelLam1",
            "ExtraLabeltype1", "Extralabelsubtype1", "ExtralabelL2", "ExtralabelW2", "ExtraLabelMicron2",
            "IsBoXExtraLabel2", "ExtraLabelLam2", "ExtraLabeltype2", "Extralabelsubtype2", "ExtralabelL3",
            "ExtralabelW3", "ExtraLabelMicron3", "IsBoXExtraLabel3", "ExtraLabelLam3", "ExtraLabeltype3",
            "Extralabelsubtype3", "BottomSpoutRopeNo", "BottomSpoutTieNo", "Boxlabelnos", "TopHookNo",
            "BottomHookNo", "NosAncerieLoop", "TopTieNo", "TopRopeNo", "BottomTieNo", "BottomRopeNo",
            "TopSpoutTieIRISNo", "BottomSpoutTieIRISNo", "LoopCoverNo", "StNo", "TopSpoutRopeNo", "TopSpoutTieNo",
            "TopTieCutSizes", "TopRopeCutSizes", "BottomTieCutSize", "BottomRopeCutSizes", "fillercordtoptype",
            "fillercordbottomtype", "fillercordFStype", "fillercordDStype", "fillercordbodytype",
            "fillercordbuffletype", "fillercordbuffle1", "fsedgehaming", "dsedgehaming", "docflap", "docflapsize",
            "mfwebbingbuffle"
        };

        private readonly DatabaseService _database;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public BomCreationService(
            DatabaseService database,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _database = database;
            _cache = cache;
        }

        public Task<BomCreatePreviewResult> PreviewAsync(BomCreateRequest request, string? userName = null)
        {
            var normalized = NormalizeRequest(request, userName);
            ValidateRequest(normalized, isUpdate: false);

            var warnings = new List<string>();
            if (normalized.Bom3Values.Count == 0)
                warnings.Add("No BOM3 editor-state values were supplied; re-opening in a future editor may be limited.");

            var previewId = Guid.NewGuid().ToString("N");
            _cache.Set(GetPreviewCacheKey(previewId), new BomPreviewSession
            {
                Detail = BuildPreviewDetail(normalized),
                PdfModel = BuildPreviewPdfModel(normalized),
            }, PreviewCacheTtl);

            return Task.FromResult(new BomCreatePreviewResult
            {
                PreviewId = previewId,
                FilePoNo = normalized.Header.FilePoNo,
                Customer = normalized.Header.Customer,
                LineCount = normalized.Lines.Count,
                TotalKg = CalculateTotalKg(normalized.Lines),
                Approvals = normalized.Approvals,
                Warnings = warnings,
                Lines = normalized.Lines,
            });
        }

        public Task<BomDetailResult?> GetPreviewDetailAsync(string previewId)
        {
            if (string.IsNullOrWhiteSpace(previewId))
                throw new ArgumentException("Preview id is required.", nameof(previewId));

            return Task.FromResult(
                _cache.TryGetValue(GetPreviewCacheKey(previewId.Trim()), out BomPreviewSession? session)
                    ? session?.Detail
                    : null);
        }

        public Task<BomPdfModel?> GetPreviewPdfModelAsync(string previewId)
        {
            if (string.IsNullOrWhiteSpace(previewId))
                throw new ArgumentException("Preview id is required.", nameof(previewId));

            return Task.FromResult(
                _cache.TryGetValue(GetPreviewCacheKey(previewId.Trim()), out BomPreviewSession? session)
                    ? session?.PdfModel
                    : null);
        }

        public async Task<BomEditorSnapshot?> GetEditorAsync(string filePoNo)
        {
            if (string.IsNullOrWhiteSpace(filePoNo))
                throw new ArgumentException("Quotation number is required.", nameof(filePoNo));

            var trimmed = filePoNo.Trim();

            using var connection = _database.CreateProductionConnection();

            var bom1Row = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT TOP 1 *
FROM BOM1 WITH (NOLOCK)
WHERE FilePONo = @FilePoNo
  AND ISNULL(SrNo, '') <> 'temp'
ORDER BY SysDate DESC, FilePONo DESC", new { FilePoNo = trimmed }, commandTimeout: BomCommandTimeoutSeconds);

            if (bom1Row is null)
                return null;

            var bom1Values = ToStringDictionary(bom1Row);
            var srNo = GetValue(bom1Values, "SrNo");

            Dictionary<string, string?> bom3Values = new(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(srNo))
            {
                var bom3Row = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT TOP 1 *
FROM BOM3 WITH (NOLOCK)
WHERE PONo = @FilePoNo
  AND SrNo = @SrNo
ORDER BY dModifyDate DESC", new { FilePoNo = trimmed, SrNo = srNo }, commandTimeout: BomCommandTimeoutSeconds);

                if (bom3Row is not null)
                    bom3Values = ToStringDictionary(bom3Row);
            }

            var lines = (await connection.QueryAsync<BomCreateLineInput>(@"
SELECT
    ISNULL(TransId, 0) AS SortOrder,
    Heading,
    GSM AS Gsm,
    Lami,
    Color,
    FabricSize,
    CutSize,
    TotalMtr,
    TotalKg,
    Remarks,
    gpm AS Gpm
FROM BOM WITH (NOLOCK)
WHERE PONo = @FilePoNo
ORDER BY ISNULL(TransId, 0), Heading", new { FilePoNo = trimmed }, commandTimeout: BomCommandTimeoutSeconds)).ToList();

            return new BomEditorSnapshot
            {
                Header = MapHeader(bom1Values),
                Approvals = ParseApprovalField(GetValue(bom1Values, "ApprovalField")),
                Lines = lines,
                Bom1Values = bom1Values,
                Bom3Values = bom3Values,
            };
        }

        public async Task<BomSaveResult> CreateAsync(BomCreateRequest request, string? userName = null)
        {
            var normalized = NormalizeRequest(request, userName);
            ValidateRequest(normalized, isUpdate: false);

            using var connection = _database.CreateProductionConnection();
            await EnsureFilePoNoAvailableAsync(connection, normalized.Header.FilePoNo);

            var srNo = Guid.NewGuid().ToString();
            var totalKg = CalculateTotalKg(normalized.Lines);

            using var tx = connection.BeginTransaction();
            try
            {
                await InsertBomRowsAsync(connection, tx, normalized.Header.FilePoNo, srNo, normalized.Header.Customer, normalized.Lines);
                await InsertBom1Async(connection, tx, normalized, srNo, totalKg);
                await InsertBom3Async(connection, tx, normalized, srNo);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            InvalidateBomCaches();
            return new BomSaveResult
            {
                FilePoNo = normalized.Header.FilePoNo,
                SrNo = srNo,
                Created = true,
                Updated = false,
                TotalKg = totalKg,
                LineCount = normalized.Lines.Count,
            };
        }

        public async Task<BomSaveResult> UpdateAsync(string filePoNo, BomCreateRequest request, string? userName = null)
        {
            if (string.IsNullOrWhiteSpace(filePoNo))
                throw new ArgumentException("Quotation number is required.", nameof(filePoNo));

            var normalized = NormalizeRequest(request, userName);
            if (string.IsNullOrWhiteSpace(normalized.Header.FilePoNo))
                normalized.Header.FilePoNo = filePoNo.Trim();

            if (!string.Equals(filePoNo.Trim(), normalized.Header.FilePoNo, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Changing quotation number during update is not allowed.");

            ValidateRequest(normalized, isUpdate: true);

            using var connection = _database.CreateProductionConnection();
            await EnsureFilePoNoExistsAsync(connection, normalized.Header.FilePoNo);
            await EnsureBomNotApprovedAsync(connection, normalized.Header.FilePoNo);

            var srNo = Guid.NewGuid().ToString();
            var totalKg = CalculateTotalKg(normalized.Lines);

            using var tx = connection.BeginTransaction();
            try
            {
                await ArchiveExistingRowsAsync(connection, tx, normalized.Header.FilePoNo);
                await DeleteExistingRowsAsync(connection, tx, normalized.Header.FilePoNo);
                await InsertBomRowsAsync(connection, tx, normalized.Header.FilePoNo, srNo, normalized.Header.Customer, normalized.Lines);
                await InsertBom1Async(connection, tx, normalized, srNo, totalKg);
                await InsertBom3Async(connection, tx, normalized, srNo);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            InvalidateBomCaches();
            return new BomSaveResult
            {
                FilePoNo = normalized.Header.FilePoNo,
                SrNo = srNo,
                Created = false,
                Updated = true,
                TotalKg = totalKg,
                LineCount = normalized.Lines.Count,
            };
        }

        private static BomCreateRequest NormalizeRequest(BomCreateRequest request, string? userName)
        {
            request ??= new BomCreateRequest();
            request.Header ??= new BomCreateHeaderInput();
            request.Lines ??= [];
            request.Approvals ??= [];
            request.Bom1Values ??= new();
            request.Bom3Values ??= new();

            var normalizedUser = NormalizeOptional(userName)
                ?? NormalizeOptional(request.Header.UserName)
                ?? "";

            request.Header.FilePoNo = NormalizeOptional(request.Header.FilePoNo) ?? "";
            request.Header.Customer = NormalizeOptional(request.Header.Customer) ?? "";
            request.Header.PrintType = NormalizeOptional(request.Header.PrintType) ?? "";
            request.Header.PoNo = NormalizeOptional(request.Header.PoNo) ?? "";
            request.Header.PoNos = NormalizeOptional(request.Header.PoNos) ?? "";
            request.Header.BagType = NormalizeOptional(request.Header.BagType) ?? "";
            request.Header.SizeType = NormalizeOptional(request.Header.SizeType) ?? "";
            request.Header.Swl = NormalizeOptional(request.Header.Swl) ?? "";
            request.Header.SfRatio = NormalizeOptional(request.Header.SfRatio) ?? "";
            request.Header.Qty = NormalizeOptional(request.Header.Qty) ?? "";
            request.Header.QtyUnit = NormalizeOptional(request.Header.QtyUnit) ?? "";
            request.Header.FSType = NormalizeOptional(request.Header.FSType) ?? "";
            request.Header.DSType = NormalizeOptional(request.Header.DSType) ?? "";
            request.Header.DSType1 = NormalizeOptional(request.Header.DSType1) ?? "";
            request.Header.DSType2 = NormalizeOptional(request.Header.DSType2) ?? "";
            request.Header.LoopType = NormalizeOptional(request.Header.LoopType) ?? "";
            request.Header.FabColor = NormalizeOptional(request.Header.FabColor) ?? "";
            request.Header.Instruction = NormalizeOptional(request.Header.Instruction) ?? "";
            request.Header.BodyRemarks = NormalizeOptional(request.Header.BodyRemarks) ?? "";
            request.Header.PrintingRemarks = NormalizeOptional(request.Header.PrintingRemarks) ?? "";
            request.Header.RefNo = NormalizeOptional(request.Header.RefNo) ?? "";
            request.Header.Doc = NormalizeOptional(request.Header.Doc) ?? "";
            request.Header.Doc1 = NormalizeOptional(request.Header.Doc1) ?? "";
            request.Header.Doc2 = NormalizeOptional(request.Header.Doc2) ?? "";
            request.Header.DocUnit = NormalizeOptional(request.Header.DocUnit) ?? "";
            request.Header.DocNumber = NormalizeOptional(request.Header.DocNumber) ?? "";
            request.Header.KnotType = NormalizeOptional(request.Header.KnotType) ?? "";
            request.Header.RpFabric = NormalizeOptional(request.Header.RpFabric) ?? "";
            request.Header.UserName = normalizedUser;
            request.Header.SysDate ??= DateTime.Today;

            request.Approvals = request.Approvals
                .Select(NormalizeOptional)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();

            request.Lines = request.Lines
                .Where(line => line is not null)
                .Select((line, index) =>
                {
                    line.Heading = NormalizeOptional(line.Heading) ?? "";
                    line.Gsm = NormalizeOptional(line.Gsm) ?? "";
                    line.Lami = NormalizeOptional(line.Lami) ?? "";
                    line.Color = NormalizeOptional(line.Color) ?? "";
                    line.FabricSize = NormalizeOptional(line.FabricSize) ?? "";
                    line.CutSize = NormalizeOptional(line.CutSize) ?? "";
                    line.Remarks = NormalizeOptional(line.Remarks) ?? "";
                    line.Gpm = NormalizeOptional(line.Gpm) ?? "";
                    if (line.SortOrder <= 0)
                        line.SortOrder = index + 1;
                    return line;
                })
                .Where(line => !string.IsNullOrWhiteSpace(line.Heading))
                .OrderBy(line => line.SortOrder)
                .ToList();

            request.Bom1Values = NormalizeDictionary(request.Bom1Values);
            request.Bom3Values = NormalizeDictionary(request.Bom3Values);
            PopulateCalculatedComponentValues(request);
            if (request.Lines.Count == 0)
                request.Lines = BuildDerivedLines(request);
            return request;
        }

        private static void ValidateRequest(BomCreateRequest request, bool isUpdate)
        {
            if (string.IsNullOrWhiteSpace(request.Header.FilePoNo))
                throw new ArgumentException("Quotation number is required.");
            if (string.IsNullOrWhiteSpace(request.Header.Customer))
                throw new ArgumentException("Customer is required.");
            if (string.IsNullOrWhiteSpace(request.Header.BagType))
                throw new ArgumentException("Bag type is required.");
            if (request.Approvals.Count == 0)
                throw new ArgumentException("At least one approval selection is required.");
            if (request.Lines.Count == 0)
                throw new ArgumentException("At least one BOM component line is required.");
            if (request.Lines.Any(line => line.TotalKg is < 0))
                throw new ArgumentException("Component weight cannot be negative.");
            if (!isUpdate && string.IsNullOrWhiteSpace(request.Header.UserName))
                throw new ArgumentException("User name is required.");
        }

        private static string GetPreviewCacheKey(string previewId) => PreviewCacheKeyPrefix + previewId;

        private static BomDetailResult BuildPreviewDetail(BomCreateRequest request)
        {
            var model = BuildPreviewPdfModel(request);
            return new BomDetailResult
            {
                Header = new BomHeader
                {
                    QtnNo = model.QtnNo,
                    PartyName = model.PartyName,
                    Date = model.Date,
                    User = model.User,
                    BagType = model.BagType,
                    SizeL = model.SizeL,
                    SizeW = model.SizeW,
                    SizeH = model.SizeH,
                    SizeType = model.SizeType,
                    Swl = model.Swl,
                    SfRatio = model.SfRatio,
                    Qty = model.Qty,
                    QtyUnit = model.QtyUnit,
                    TotalKg = model.TotalKgPerBag,
                    PrintType = model.PrintType,
                    PoNo = model.PoNo,
                    PoNos = model.PoNos,
                    SrNo = "",
                    Instruction = model.Instruction,
                    RefNo = model.RefNo,
                    Doc = model.Doc,
                    Doc1 = model.Doc1,
                    Doc2 = model.Doc2,
                    LoopSpec = model.LoopSpec,
                    LinerSpec = model.LinerSpec,
                    TopSpoutType = model.TopSpoutType,
                    BottomType = model.BottomType,
                    FabColor = model.FabColor,
                    PrintingRemarks = model.PrintingRemarks,
                    BodyRemarks = model.BodyRemarks,
                    MarketingInvNo = model.MarketingInvNo,
                    IsDropLoop = model.IsDropLoop,
                    RpFabric = model.RpFabric,
                    KnotType = model.KnotType,
                },
                Lines = model.Lines.Select(line => new BomLineItem
                {
                    SortOrder = line.SortOrder,
                    Heading = line.Heading,
                    Gsm = line.Gsm,
                    Lami = line.Lami,
                    Color = line.Color,
                    FabricSize = line.FabricSize,
                    CutSize = line.CutSize,
                    TotalMtr = line.TotalMtr,
                    TotalKg = line.TotalKg,
                    Gpm = line.Gpm,
                    Remarks = line.Remarks,
                }).ToList(),
                ReportLines = Array.Empty<BomReportLine>(),
            };
        }

        private static BomPdfModel BuildPreviewPdfModel(BomCreateRequest request)
        {
            var header = request.Header;
            return new BomPdfModel
            {
                QtnNo = header.FilePoNo,
                PartyName = header.Customer,
                Date = header.SysDate,
                User = header.UserName,
                RefNo = header.RefNo,
                PoNo = header.PoNo,
                PoNos = header.PoNos,
                MarketingInvNo = "",
                BagType = header.BagType,
                SizeType = header.SizeType,
                SizeL = header.SizeL,
                SizeW = header.SizeW,
                SizeH = header.SizeH,
                Swl = header.Swl,
                SfRatio = FirstNonEmpty(header.SfRatio, GetValue(request.Bom1Values, "L")),
                Qty = header.Qty,
                QtyUnit = header.QtyUnit,
                PrintType = header.PrintType,
                Doc = NormalizeDocValue(header.Doc),
                Doc1 = NormalizeDocValue(header.Doc1),
                Doc2 = NormalizeDocValue(header.Doc2),
                LoopSpec = BuildPreviewLoopSpec(request),
                LinerSpec = BuildPreviewLinerSpec(request),
                TopSpoutType = header.FSType,
                BottomType = BuildPreviewBottomSpec(request),
                FabColor = header.FabColor,
                IsDropLoop = header.IsDropLoop ? "yes" : "no",
                RpFabric = header.RpFabric,
                KnotType = header.KnotType,
                TotalKgPerBag = CalculateTotalKg(request.Lines),
                Instruction = header.Instruction,
                PrintingRemarks = header.PrintingRemarks,
                BodyRemarks = header.BodyRemarks,
                Lines = request.Lines
                    .OrderBy(line => line.SortOrder)
                    .Select(line => new BomPdfLine
                    {
                        SortOrder = line.SortOrder,
                        Heading = line.Heading,
                        Gsm = line.Gsm,
                        Lami = line.Lami,
                        Color = line.Color,
                        FabricSize = line.FabricSize,
                        CutSize = line.CutSize,
                        TotalMtr = line.TotalMtr,
                        TotalKg = line.TotalKg,
                        Gpm = line.Gpm,
                        Remarks = line.Remarks,
                    })
                    .ToList(),
            };
        }

        private static string BuildPreviewLoopSpec(BomCreateRequest request)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Header.LoopType))
                parts.Add(request.Header.LoopType);

            var loopL = GetValue(request.Bom1Values, "LoopL");
            var loopW = GetValue(request.Bom1Values, "LoopW");
            var loopDim = GetValue(request.Bom1Values, "LoopDim");
            if (!string.IsNullOrWhiteSpace(loopL)) parts.Add($"L {loopL}");
            if (!string.IsNullOrWhiteSpace(loopW)) parts.Add($"W {loopW}");
            if (!string.IsNullOrWhiteSpace(loopDim)) parts.Add(loopDim);
            return string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private static string BuildPreviewLinerSpec(BomCreateRequest request)
        {
            var liner = GetValue(request.Bom1Values, "Liner");
            var linerL = GetValue(request.Bom1Values, "LinerL");
            var linerW = GetValue(request.Bom1Values, "LinerW");
            var linerDim = GetValue(request.Bom1Values, "LinerDim");
            if (string.IsNullOrWhiteSpace(liner) && string.IsNullOrWhiteSpace(linerL))
                return "";

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(liner)) parts.Add(liner);
            if (!string.IsNullOrWhiteSpace(linerL) || !string.IsNullOrWhiteSpace(linerW))
                parts.Add($"{linerL} × {linerW} {linerDim}".Trim());
            return string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private static string BuildPreviewBottomSpec(BomCreateRequest request)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Header.DSType)) parts.Add(request.Header.DSType);

            var dsl = GetValue(request.Bom1Values, "DSL");
            var dsw = GetValue(request.Bom1Values, "DSW");
            if (!string.IsNullOrWhiteSpace(dsl) || !string.IsNullOrWhiteSpace(dsw))
                parts.Add($"{dsl} × {dsw}".Trim());
            return string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private static string NormalizeDocValue(string value) =>
            string.IsNullOrWhiteSpace(value) ? "N/A" : value;

        private async Task EnsureFilePoNoAvailableAsync(SqlConnection connection, string filePoNo)
        {
            var exists = await connection.ExecuteScalarAsync<int>(@"
SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM BOM1 WITH (NOLOCK)
    WHERE FilePONo = @FilePoNo
      AND ISNULL(SrNo, '') <> 'temp'
) THEN 1 ELSE 0 END", new { FilePoNo = filePoNo }, commandTimeout: BomCommandTimeoutSeconds);

            if (exists == 1)
                throw new InvalidOperationException("This quotation number is already stored in BOM1.");
        }

        private async Task EnsureFilePoNoExistsAsync(SqlConnection connection, string filePoNo)
        {
            var exists = await connection.ExecuteScalarAsync<int>(@"
SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM BOM1 WITH (NOLOCK)
    WHERE FilePONo = @FilePoNo
      AND ISNULL(SrNo, '') <> 'temp'
) THEN 1 ELSE 0 END", new { FilePoNo = filePoNo }, commandTimeout: BomCommandTimeoutSeconds);

            if (exists != 1)
                throw new InvalidOperationException("BOM not found for this quotation number.");
        }

        private async Task EnsureBomNotApprovedAsync(SqlConnection connection, string filePoNo)
        {
            var approved = await connection.ExecuteScalarAsync<int>(@"
SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM approvebom WITH (NOLOCK)
    WHERE filepono = @FilePoNo
      AND LOWER(ISNULL(status, '')) = 'approved'
) THEN 1 ELSE 0 END", new { FilePoNo = filePoNo }, commandTimeout: BomCommandTimeoutSeconds);

            if (approved == 1)
                throw new InvalidOperationException("BOM is already approved and cannot be updated.");
        }

        private async Task ArchiveExistingRowsAsync(SqlConnection connection, SqlTransaction tx, string filePoNo)
        {
            await connection.ExecuteAsync("INSERT INTO BOM1_delete SELECT * FROM BOM1 WHERE FilePONo = @FilePoNo", new { FilePoNo = filePoNo }, tx, BomCommandTimeoutSeconds);
            await connection.ExecuteAsync("INSERT INTO BOM_delete SELECT * FROM BOM WHERE PONo = @FilePoNo", new { FilePoNo = filePoNo }, tx, BomCommandTimeoutSeconds);
            await connection.ExecuteAsync("INSERT INTO BOM2_delete SELECT * FROM BOM2 WHERE PONo = @FilePoNo", new { FilePoNo = filePoNo }, tx, BomCommandTimeoutSeconds);
            await connection.ExecuteAsync("INSERT INTO BOM3_delete SELECT * FROM BOM3 WHERE PONo = @FilePoNo", new { FilePoNo = filePoNo }, tx, BomCommandTimeoutSeconds);
        }

        private async Task DeleteExistingRowsAsync(SqlConnection connection, SqlTransaction tx, string filePoNo)
        {
            await connection.ExecuteAsync("DELETE FROM BOM1 WHERE FilePONo = @FilePoNo", new { FilePoNo = filePoNo }, tx, BomCommandTimeoutSeconds);
            await connection.ExecuteAsync("DELETE FROM BOM2 WHERE PONo = @FilePoNo", new { FilePoNo = filePoNo }, tx, BomCommandTimeoutSeconds);
            await connection.ExecuteAsync("DELETE FROM BOM3 WHERE PONo = @FilePoNo", new { FilePoNo = filePoNo }, tx, BomCommandTimeoutSeconds);
            await connection.ExecuteAsync("DELETE FROM BOM WHERE PONo = @FilePoNo", new { FilePoNo = filePoNo }, tx, BomCommandTimeoutSeconds);
        }

        private static async Task InsertBomRowsAsync(
            SqlConnection connection,
            SqlTransaction tx,
            string filePoNo,
            string srNo,
            string customer,
            IReadOnlyList<BomCreateLineInput> lines)
        {
            const string sql = @"
INSERT INTO BOM
(
    Heading, GSM, Lami, Color, FabricSize, CutSize,
    TotalMtr, TotalKg, PartyName, PONo, SrNo, Remarks, gpm, ModifyDate
)
VALUES
(
    @Heading, @Gsm, @Lami, @Color, @FabricSize, @CutSize,
    @TotalMtr, @TotalKg, @PartyName, @PONo, @SrNo, @Remarks, @Gpm, @ModifyDate
)";

            var now = DateTime.Now;
            foreach (var line in lines)
            {
                await connection.ExecuteAsync(sql, new
                {
                    line.Heading,
                    Gsm = EmptyToNull(line.Gsm),
                    Lami = EmptyToNull(line.Lami),
                    Color = EmptyToNull(line.Color),
                    FabricSize = EmptyToNull(line.FabricSize),
                    CutSize = EmptyToNull(line.CutSize),
                    line.TotalMtr,
                    line.TotalKg,
                    PartyName = customer,
                    PONo = filePoNo,
                    SrNo = srNo,
                    Remarks = EmptyToNull(line.Remarks),
                    Gpm = EmptyToNull(line.Gpm),
                    ModifyDate = now,
                }, tx, BomCommandTimeoutSeconds);
            }
        }

        private static async Task InsertBom1Async(
            SqlConnection connection,
            SqlTransaction tx,
            BomCreateRequest request,
            string srNo,
            double totalKg)
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SysDate"] = request.Header.SysDate?.Date ?? DateTime.Today,
                ["Customer"] = request.Header.Customer,
                ["PrintType"] = EmptyToNull(request.Header.PrintType),
                ["FilePONo"] = request.Header.FilePoNo,
                ["BagType"] = request.Header.BagType,
                ["SizeL"] = request.Header.SizeL,
                ["SizeW"] = request.Header.SizeW,
                ["SizeH"] = request.Header.SizeH,
                ["SizeType"] = EmptyToNull(request.Header.SizeType),
                ["SWL"] = EmptyToNull(request.Header.Swl),
                ["S"] = EmptyToNull(request.Header.SfRatio),
                ["Qty"] = EmptyToNull(request.Header.Qty),
                ["QtyUnit"] = EmptyToNull(request.Header.QtyUnit),
                ["FSType"] = EmptyToNull(request.Header.FSType),
                ["DSType"] = EmptyToNull(request.Header.DSType),
                ["DSType1"] = EmptyToNull(request.Header.DSType1),
                ["DSType2"] = EmptyToNull(request.Header.DSType2),
                ["FabColor"] = EmptyToNull(request.Header.FabColor),
                ["TotalKg"] = totalKg,
                ["Instruction"] = EmptyToNull(request.Header.Instruction),
                ["BodyRemarks1"] = EmptyToNull(request.Header.BodyRemarks),
                ["SrNo"] = srNo,
                ["pono"] = EmptyToNull(request.Header.PoNo),
                ["ponos"] = EmptyToNull(request.Header.PoNos) ?? EmptyToNull(request.Header.PoNo),
                ["printingremarks"] = EmptyToNull(request.Header.PrintingRemarks),
                ["refNo"] = EmptyToNull(request.Header.RefNo),
                ["UserName"] = EmptyToNull(request.Header.UserName),
                ["Doc"] = string.IsNullOrWhiteSpace(request.Header.Doc) ? "N/A" : request.Header.Doc,
                ["Doc1"] = string.IsNullOrWhiteSpace(request.Header.Doc1) ? "N/A" : request.Header.Doc1,
                ["Doc2"] = string.IsNullOrWhiteSpace(request.Header.Doc2) ? "N/A" : request.Header.Doc2,
                ["DocUnit"] = EmptyToNull(request.Header.DocUnit),
                ["DocNumber"] = EmptyToNull(request.Header.DocNumber),
                ["looptype"] = EmptyToNull(request.Header.LoopType),
                ["IsDropLoop"] = request.Header.IsDropLoop ? "yes" : "no",
                ["ApprovalField"] = BuildApprovalField(request.Approvals),
                ["Knottype"] = EmptyToNull(request.Header.KnotType),
                ["RPfabric"] = EmptyToNull(request.Header.RpFabric),
            };

            foreach (var (key, value) in request.Bom1Values)
            {
                if (ReservedBom1Columns.Contains(key) || !AllowedBom1ExtraColumns.Contains(key))
                    continue;
                values[key] = value;
            }

            await InsertDynamicRowAsync(connection, tx, "BOM1", values);
        }

        private static async Task InsertBom3Async(
            SqlConnection connection,
            SqlTransaction tx,
            BomCreateRequest request,
            string srNo)
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SrNo"] = srNo,
                ["PONO"] = request.Header.FilePoNo,
                ["dModifyDate"] = DateTime.Now,
            };

            foreach (var (key, value) in request.Bom3Values)
            {
                if (ReservedBom3Columns.Contains(key) || !AllowedBom3Columns.Contains(key))
                    continue;
                values[key] = value;
            }

            await InsertDynamicRowAsync(connection, tx, "BOM3", values);
        }

        private static async Task InsertDynamicRowAsync(
            SqlConnection connection,
            SqlTransaction tx,
            string tableName,
            IReadOnlyDictionary<string, object?> values)
        {
            var columns = values.Keys.ToList();
            if (columns.Count == 0)
                throw new InvalidOperationException($"No values were provided for insert into {tableName}.");

            var sql = $@"
INSERT INTO {tableName}
(
    {string.Join(", ", columns.Select(QuoteIdentifier))}
)
VALUES
(
    {string.Join(", ", columns.Select(c => "@" + c))}
)";

            var parameters = new DynamicParameters();
            foreach (var (key, value) in values)
                parameters.Add("@" + key, value);

            await connection.ExecuteAsync(sql, parameters, tx, BomCommandTimeoutSeconds);
        }

        private static string QuoteIdentifier(string value) => $"[{value.Replace("]", "]]")}]";

        private static BomCreateHeaderInput MapHeader(IReadOnlyDictionary<string, string?> row)
        {
            return new BomCreateHeaderInput
            {
                FilePoNo = GetValue(row, "FilePONo"),
                Customer = GetValue(row, "Customer"),
                SysDate = ParseDate(GetValue(row, "SysDate")),
                PrintType = GetValue(row, "PrintType"),
                PoNo = GetValue(row, "pono"),
                PoNos = GetValue(row, "ponos"),
                BagType = GetValue(row, "BagType"),
                SizeL = ParseDouble(GetValue(row, "SizeL")),
                SizeW = ParseDouble(GetValue(row, "SizeW")),
                SizeH = ParseDouble(GetValue(row, "SizeH")),
                SizeType = GetValue(row, "SizeType"),
                Swl = GetValue(row, "SWL"),
                SfRatio = GetValue(row, "S"),
                Qty = GetValue(row, "Qty"),
                QtyUnit = GetValue(row, "QtyUnit"),
                FSType = GetValue(row, "FSType"),
                DSType = GetValue(row, "DSType"),
                DSType1 = GetValue(row, "DSType1"),
                DSType2 = GetValue(row, "DSType2"),
                LoopType = GetValue(row, "looptype"),
                FabColor = GetValue(row, "FabColor"),
                Instruction = GetValue(row, "Instruction"),
                BodyRemarks = GetValue(row, "BodyRemarks1"),
                PrintingRemarks = GetValue(row, "printingremarks"),
                RefNo = GetValue(row, "refNo"),
                Doc = GetValue(row, "Doc"),
                Doc1 = GetValue(row, "Doc1"),
                Doc2 = GetValue(row, "Doc2"),
                DocUnit = GetValue(row, "DocUnit"),
                DocNumber = GetValue(row, "DocNumber"),
                KnotType = GetValue(row, "Knottype"),
                RpFabric = GetValue(row, "RPfabric"),
                IsDropLoop = string.Equals(GetValue(row, "IsDropLoop"), "yes", StringComparison.OrdinalIgnoreCase),
                UserName = GetValue(row, "UserName"),
            };
        }

        private static Dictionary<string, string?> ToStringDictionary(dynamic row)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (row is not IDictionary<string, object> values)
                return result;

            foreach (var (key, value) in values)
                result[key] = value switch
                {
                    null or DBNull => null,
                    DateTime dateTime => dateTime.ToString("yyyy-MM-dd"),
                    _ => Convert.ToString(value),
                };

            return result;
        }

        private static Dictionary<string, string?> NormalizeDictionary(Dictionary<string, string?> values)
        {
            var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in values)
            {
                var trimmedKey = NormalizeOptional(key);
                if (string.IsNullOrWhiteSpace(trimmedKey))
                    continue;
                normalized[trimmedKey] = NormalizeOptional(value);
            }
            return normalized;
        }

        private static IReadOnlyList<string> ParseApprovalField(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            return value
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildApprovalField(IEnumerable<string> approvals)
        {
            var rows = approvals
                .Select(NormalizeOptional)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();

            return rows.Count == 0 ? "" : string.Join(Environment.NewLine, rows);
        }

        private static void PopulateCalculatedComponentValues(BomCreateRequest request)
        {
            PopulateCalculatedBodyValues(request);
            PopulateCalculatedSideValues(request);
            PopulateCalculatedTopValues(request);
            PopulateCalculatedTopSpoutValues(request);
            PopulateCalculatedTopSpoutTieValues(request);
            PopulateCalculatedTopSpoutIrisTieValues(request);
            PopulateCalculatedBottomValues(request);
            PopulateCalculatedBottomSpoutValues(request);
            PopulateCalculatedBottomSpoutTieValues(request);
            PopulateCalculatedBottomSpoutIrisTieValues(request);
            PopulateCalculatedLoopValues(request);
            PopulateCalculatedLinerValues(request);
            PopulateCalculatedDocValues(request);
            PopulateCalculatedLabelValues(request);
            PopulateCalculatedFillerCordValues(request);
            PopulateCalculatedThreadValues(request);
        }

        private static void PopulateCalculatedBodyValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "BodyTotalKg")))
                return;

            var length = request.Header.SizeL ?? 0;
            var width = request.Header.SizeW ?? 0;
            var height = request.Header.SizeH ?? 0;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            var bodyNo = ParseDouble(GetValue(values, "bodyno")) ?? 1;
            var gsm = ParseDouble(GetValue(values, "BodyGSM")) ?? 0;
            var lami = ParseDouble(GetValue(values, "BodyLami")) ?? 0;
            if (length <= 0 || width <= 0 || height <= 0 || qty <= 0 || gsm + lami <= 0)
                return;

            var (construction, bodyStyle, bodyGrade) = ResolveBagTypeParts(request);
            var isInner = IsInnerPlacement(request);
            var isDoubleFoldBody = IsTruthy(GetValue(request.Bom3Values, "DoubleFoldBody"));
            var isTunnel = construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase)
                && (bodyStyle.Contains("Tunnel", StringComparison.OrdinalIgnoreCase)
                    || GetValue(request.Bom3Values, "TunnelDesign").Length > 0);

            double bodyFabric = 0;
            double bodyCut = 0;
            double bodyWeightRaw = 0;
            double? tunnelFabric = null;
            double? tunnelCut = null;
            double? tunnelWeight = null;
            double? tunnelTotalMtr = null;

            if (construction.Equals("Circular", StringComparison.OrdinalIgnoreCase)
                || construction.Equals("Double Layer Circular Inner Skin Bag", StringComparison.OrdinalIgnoreCase))
            {
                bodyFabric = length + width;
                bodyCut = height + (isInner ? 11 : 7);
                if (isDoubleFoldBody)
                    bodyCut = height + (isInner ? 18 : 14);
                bodyWeightRaw = bodyCut * bodyFabric * 2 * (gsm + lami);
            }
            else if (construction.Equals("4 Panel", StringComparison.OrdinalIgnoreCase))
            {
                var isVentilated = bodyStyle.Contains("Ventilated", StringComparison.OrdinalIgnoreCase)
                    || bodyStyle.Contains("Sulzer", StringComparison.OrdinalIgnoreCase);

                if (isVentilated)
                {
                    if (isInner)
                    {
                        bodyFabric = length + 4;
                        bodyCut = height + 11;
                        if (isDoubleFoldBody)
                        {
                            bodyFabric = width + 18;
                            bodyCut = height + 18;
                        }
                    }
                    else
                    {
                        bodyFabric = length;
                        bodyCut = height + 7;
                        if (isDoubleFoldBody)
                        {
                            bodyFabric = width + 7;
                            bodyCut = (height * 2) + length + 14;
                        }
                    }
                }
                else
                {
                    if (isInner)
                    {
                        bodyFabric = length + 11;
                        bodyCut = height + 11;
                        if (isDoubleFoldBody)
                        {
                            bodyFabric = length + 18;
                            bodyCut = height + 18;
                        }
                    }
                    else
                    {
                        bodyFabric = length + 7;
                        bodyCut = height + 7;
                        if (isDoubleFoldBody)
                        {
                            bodyFabric = width + 14;
                            bodyCut = height + 14;
                        }
                    }
                }

                var panelFactor = NearlyEqual(length, width) ? 4 : 2;
                bodyWeightRaw = bodyCut * (gsm + lami) * bodyFabric * panelFactor;
            }
            else if (construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Double Layer Tunnel Lift Loop Bag", StringComparison.OrdinalIgnoreCase))
            {
                if (isTunnel)
                {
                    var tunnelDesign = FirstNonEmpty(
                        GetValue(request.Bom3Values, "TunnelDesign"),
                        GetValue(values, "TunnelDesign"),
                        "Flexcon");

                    if (tunnelDesign.Equals("Flexcon", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isInner)
                        {
                            bodyFabric = width + (isDoubleFoldBody ? 18 : 15);
                            bodyCut = isDoubleFoldBody
                                ? ((height + 18) * 2) + length + 300
                                : (height * 2) + length + 150;
                        }
                        else
                        {
                            bodyFabric = width + (isDoubleFoldBody ? 18 : 10);
                            bodyCut = isDoubleFoldBody
                                ? ((height * 2) + length) + 158
                                : ((height * 2) + length) + 140;
                        }

                        tunnelFabric = bodyFabric;
                        tunnelCut = 22;
                    }
                    else if (tunnelDesign.Contains("Store", StringComparison.OrdinalIgnoreCase)
                             || tunnelDesign.Contains("Plastene", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isInner)
                        {
                            bodyFabric = isDoubleFoldBody ? width + 18 : width + 15;
                            bodyCut = isDoubleFoldBody
                                ? (height * 2) + length + 170
                                : (height * 2) + length + 152;
                        }
                        else
                        {
                            bodyFabric = isDoubleFoldBody ? width + 18 : width + 10;
                            bodyCut = isDoubleFoldBody
                                ? ((height * 2) + length) + 149
                                : ((height * 2) + length) + 142;
                        }

                        tunnelFabric = bodyFabric;
                        tunnelCut = 28;
                    }
                    else if (tunnelDesign.Contains("Greif", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isInner)
                        {
                            bodyFabric = isDoubleFoldBody ? width + 27 : width + 20;
                            bodyCut = isDoubleFoldBody
                                ? (((height * 2) + length) * 2) + 60
                                : (((height * 2) + length) * 2) + 42;
                        }
                        else
                        {
                            bodyFabric = isDoubleFoldBody ? width + 18 : width + 16;
                            bodyCut = isDoubleFoldBody
                                ? (((height * 2) + length) * 2) + 60
                                : (((height * 2) + length) * 2) + 42;
                        }

                        tunnelFabric = bodyFabric;
                        tunnelCut = 22;
                    }

                    bodyWeightRaw = bodyCut * (gsm + lami) * bodyFabric;

                    var tunnelGsm = ParseDouble(GetValue(values, "TunnelGSM")) ?? 0;
                    var tunnelLami = ParseDouble(GetValue(values, "TunnelLami")) ?? 0;
                    if (tunnelFabric is > 0 && tunnelCut is > 0 && tunnelGsm + tunnelLami > 0)
                    {
                        tunnelWeight = Math.Round((tunnelCut.Value * (tunnelGsm + tunnelLami) * tunnelFabric.Value * 2) / 10000000, 4);
                        tunnelTotalMtr = Math.Round((tunnelCut.Value * qty * 2) / 100, 4);
                    }
                }
                else
                {
                    var isWiderFold = bodyStyle.Contains("Wider Fold", StringComparison.OrdinalIgnoreCase);
                    var isVentilated = bodyStyle.Contains("Ventilated", StringComparison.OrdinalIgnoreCase)
                        || bodyStyle.Contains("Sulzer", StringComparison.OrdinalIgnoreCase);
                    var isTunnelCenterJoint = bodyStyle.Contains("TUNNEL CENTER JOINT", StringComparison.OrdinalIgnoreCase);
                    var isUnGrade = bodyGrade.Equals("UN", StringComparison.OrdinalIgnoreCase)
                        || bodyGrade.Equals("UN+FDA", StringComparison.OrdinalIgnoreCase);

                    if (isWiderFold)
                    {
                        if (isInner)
                        {
                            bodyFabric = isDoubleFoldBody ? width + 18 : length + 13;
                            bodyCut = isDoubleFoldBody ? (height * 2) + width + 18 : (height * 2) + width + 15;
                        }
                        else
                        {
                            bodyFabric = isDoubleFoldBody ? width + 18 : length + 10;
                            bodyCut = isDoubleFoldBody ? (height * 2) + width + 15 : (height * 2) + width + 8;
                        }
                    }
                    else if (isVentilated)
                    {
                        if (isInner)
                        {
                            bodyFabric = isDoubleFoldBody ? length + 12 : length + 4;
                            bodyCut = isDoubleFoldBody ? (height * 2) + width + 21 : (height * 2) + width + 15;
                        }
                        else
                        {
                            bodyFabric = isDoubleFoldBody ? length + 8 : length;
                            bodyCut = isDoubleFoldBody ? (height * 2) + width + 14 : (height * 2) + width + 7;
                        }
                    }
                    else if (isTunnelCenterJoint)
                    {
                        bodyFabric = isInner ? width + 20 : width + 20;
                        bodyCut = (((height * 2) + length) * 2) + 42;
                        if (isDoubleFoldBody)
                        {
                            bodyFabric = width + (isInner ? 18 : 27);
                            bodyCut += 18;
                        }
                    }
                    else if (isUnGrade)
                    {
                        if (isInner)
                        {
                            bodyFabric = isDoubleFoldBody ? width + 18 : length + 15;
                            bodyCut = isDoubleFoldBody ? (height * 2) + width + 18 : (height * 2) + width + 19;
                        }
                        else
                        {
                            bodyFabric = isDoubleFoldBody ? width + 18 : length + 10;
                            bodyCut = isDoubleFoldBody ? (height * 2) + width + 15 : (height * 2) + width + 8;
                        }
                    }
                    else
                    {
                        if (isInner)
                        {
                            bodyFabric = isDoubleFoldBody ? length + 18 : length + 11;
                            bodyCut = isDoubleFoldBody ? (height * 2) + width + 23 : (height * 2) + width + 15;
                        }
                        else
                        {
                            bodyFabric = isDoubleFoldBody ? width + 14 : length + 7;
                            bodyCut = isDoubleFoldBody ? (height * 2) + width + 13 : (height * 2) + width + 6;
                        }
                    }

                    bodyWeightRaw = bodyCut * (gsm + lami) * bodyFabric;
                }
            }

            if (bodyFabric <= 0 || bodyCut <= 0 || bodyWeightRaw <= 0)
                return;

            var bodyFactor = construction.Equals("4 Panel", StringComparison.OrdinalIgnoreCase)
                ? (NearlyEqual(length, width) ? 4 : 2)
                : 1;

            var bodyTotalKg = Math.Round(bodyWeightRaw / 10000000, 4, MidpointRounding.AwayFromZero);
            var bodyTotalMtr = Math.Round(((bodyCut / 100) * qty * bodyFactor), 4, MidpointRounding.AwayFromZero) * bodyNo;

            SetIfMissing(values, "BodyFabric", bodyFabric);
            SetIfMissing(values, "BodyCutSize", bodyCut);
            SetIfMissing(values, "BodyTotalKg", bodyTotalKg);
            SetIfMissing(values, "BodyTotalMtr", bodyTotalMtr);

            if (tunnelFabric is > 0)
                SetIfMissing(values, "TunnelFabric", tunnelFabric.Value);
            if (tunnelCut is > 0)
                SetIfMissing(values, "TunnelCutSize", tunnelCut.Value);
            if (tunnelWeight is > 0)
                SetIfMissing(values, "TunnelTotalKg", tunnelWeight.Value);
            if (tunnelTotalMtr is > 0)
                SetIfMissing(values, "TunnelTotalMtr", tunnelTotalMtr.Value);
        }

        private static void PopulateCalculatedLoopValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "LoopTotalKg")))
                return;

            var loopGrm = ParseDouble(GetValue(values, "LoopGSM")) ?? 0;
            var loopL = ParseDouble(GetValue(values, "LoopL")) ?? 0;
            var loopW = ParseDouble(GetValue(values, "LoopW")) ?? 0;
            var loopNo = ParseDouble(GetValue(values, "loopRemarks")) ?? 0;
            var bagHeight = request.Header.SizeH ?? 0;
            var bagLength = request.Header.SizeL ?? 0;
            var bagWidth = request.Header.SizeW ?? 0;
            var bagQty = ParseDouble(request.Header.Qty) ?? 0;
            var swl = ParseDouble(request.Header.Swl) ?? 0;
            if (loopGrm <= 0 || loopL <= 0 || loopW <= 0 || loopNo <= 0 || bagQty <= 0)
                return;

            var (construction, bodyStyle, _) = ResolveBagTypeParts(request);
            var loopConst = GetValue(values, "loopconst");
            var loopOverride = ParseDouble(GetValue(request.Bom3Values, "LoopLength"));
            var dropLoopLength = ParseDouble(GetValue(values, "DropLoop")) ?? 0;
            var hasDropLoop = request.Header.IsDropLoop;
            var hasTunnel = construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase)
                && bodyStyle.Contains("Tunnel", StringComparison.OrdinalIgnoreCase);
            var loopTillBottom = IsTruthy(GetValue(request.Bom3Values, "TillTheBottom"));
            var sfBucket = ResolveSfBucket(request.Header.SfRatio);

            double loopCut = 0;

            var isTillBottomConstruction = construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase)
                || construction.Equals("Buffle", StringComparison.OrdinalIgnoreCase)
                || construction.Equals("4 Panel", StringComparison.OrdinalIgnoreCase)
                || construction.Equals("Tube + Corner", StringComparison.OrdinalIgnoreCase)
                || construction.Equals("Single + 4 Loop", StringComparison.OrdinalIgnoreCase)
                || construction.Equals("Double + 4 Loop", StringComparison.OrdinalIgnoreCase)
                || construction.Equals("4 Panel + Conical Bag(Three Piece)", StringComparison.OrdinalIgnoreCase)
                || construction.Equals("4 Panel + Conical Bag(Single Piece)", StringComparison.OrdinalIgnoreCase);

            if (isTillBottomConstruction && loopTillBottom)
            {
                loopCut = sfBucket switch
                {
                    5 => (loopL * 2) + 50 + (bagHeight - 5),
                    _ => (loopL * 2) + 60 + (bagHeight - 5),
                };

                if (hasDropLoop)
                    loopCut = loopCut - dropLoopLength + (dropLoopLength * 2);

                if (hasTunnel && bagHeight <= 100)
                    loopCut = 160;
                else if (hasTunnel && bagHeight > 100)
                    loopCut = (loopL * 2) + 50 + (0.75 * bagHeight);
            }
            else if (construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase)
                     && bodyStyle.Equals("Builder", StringComparison.OrdinalIgnoreCase))
            {
                if (swl <= 1000)
                    loopCut = (loopL * 2) + 40 + (0.66 * bagHeight);
                else if ((sfBucket == 5 || sfBucket == 6) && swl <= 1500)
                    loopCut = (loopL * 2) + 50 + (0.75 * bagHeight);
                else
                    loopCut = (loopL * 2) + 60 + (0.80 * bagHeight);

                if (hasDropLoop)
                    loopCut += dropLoopLength * 2;

                if (hasTunnel && bagHeight <= 90)
                    loopCut = 160;
                else if (hasTunnel && bagHeight > 90)
                    loopCut = (loopL * 2) + 50 + (0.75 * bagHeight);
            }
            else if (loopConst.Equals("Cross Corner", StringComparison.OrdinalIgnoreCase)
                     || loopConst.Equals("Full Loop + Cross Corner", StringComparison.OrdinalIgnoreCase))
            {
                var extra = ResolveCrossCornerExtra(sfBucket, swl);
                if (extra <= 0)
                    return;
                loopCut = (loopL * 2) + extra;
                if (hasDropLoop)
                    loopCut += dropLoopLength * 2;
            }

            if (loopOverride is > 0)
                loopCut = loopOverride.Value;

            if (loopCut <= 0)
                return;

            var loopTotalKg = Math.Round((loopCut * loopGrm * loopNo) / 100000, 4, MidpointRounding.AwayFromZero);
            var loopTotalMtr = Math.Round((loopCut / 100) * bagQty * loopNo, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "LoopFabric", loopW);
            SetIfMissing(values, "LoopCutSize", loopCut);
            SetIfMissing(values, "LoopTotalKg", loopTotalKg);
            SetIfMissing(values, "LoopTotalMtr", loopTotalMtr);
        }

        private static void PopulateCalculatedTopValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "TopTotalKg")))
                return;

            var length = request.Header.SizeL ?? 0;
            var width = request.Header.SizeW ?? 0;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            var gsm = ParseDouble(GetValue(values, "TopGSM")) ?? 0;
            var lami = ParseDouble(GetValue(values, "TopLami")) ?? 0;
            if (length <= 0 || width <= 0 || qty <= 0 || gsm + lami <= 0)
                return;

            var (construction, bodyStyle, _) = ResolveBagTypeParts(request);
            var isInner = IsInnerPlacement(request);
            var isDoubleFoldTop = IsTruthy(GetValue(request.Bom3Values, "DoubleFoldTop"));
            var topType = FirstNonEmpty(GetValue(request.Bom3Values, "toptypes"), "Open");
            var duffleHeight = ParseDouble(GetValue(values, "DuffleHt")) ?? 0;
            var conicalTop = ParseDouble(GetValue(values, "conicaltop")) ?? 0;

            double topFabric = 0;
            double topCut = 0;
            double factor = 1;

            if (topType.Equals("Conical PlateTop", StringComparison.OrdinalIgnoreCase))
            {
                topFabric = length + conicalTop + (isDoubleFoldTop ? 18 : 0);
                topCut = width + conicalTop + (isDoubleFoldTop ? 18 : 0);
            }
            else if (topType.Equals("Conical Top", StringComparison.OrdinalIgnoreCase))
            {
                topFabric = length + (isInner ? (isDoubleFoldTop ? 18 : 12) : (isDoubleFoldTop ? 14 : 8)) + (conicalTop * 2);
                topCut = topFabric;
            }
            else if (topType.Equals("Duffle or Skrit", StringComparison.OrdinalIgnoreCase)
                     || topType.Equals("Top + Skrit", StringComparison.OrdinalIgnoreCase)
                     || topType.Equals("Oversize Duffle or Skrit", StringComparison.OrdinalIgnoreCase)
                     || topType.Equals("Drawstring Skirt", StringComparison.OrdinalIgnoreCase)
                     || topType.Equals("Jute Skirt", StringComparison.OrdinalIgnoreCase)
                     || topType.Equals("Leno", StringComparison.OrdinalIgnoreCase))
            {
                if (duffleHeight <= 0)
                    return;

                if (topType.Equals("Leno", StringComparison.OrdinalIgnoreCase)
                    || topType.Equals("Jute Skirt", StringComparison.OrdinalIgnoreCase))
                {
                    topFabric = duffleHeight + (isDoubleFoldTop ? 18 : 10);
                    topCut = ((length + width) * 2) + (isDoubleFoldTop ? 23 : 16);
                }
                else if (topType.Equals("Drawstring Skirt", StringComparison.OrdinalIgnoreCase))
                {
                    topFabric = duffleHeight + (isDoubleFoldTop ? 12 : 15);
                    topCut = (((isInner ? length : length - 4) + (isInner ? width : width - 4)) * 2) + (isDoubleFoldTop ? 14 : 12);
                }
                else
                {
                    var topLam = lami > 0;
                    if (construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase)
                        || construction.Equals("4 Panel", StringComparison.OrdinalIgnoreCase))
                    {
                        topFabric = duffleHeight + (isDoubleFoldTop ? 12 : 5);
                        topCut = (((isInner ? length : length - 4) + (isInner ? width : width - 4)) * 2) + (isDoubleFoldTop ? 14 : 12);
                    }
                    else if (construction.Equals("Circular", StringComparison.OrdinalIgnoreCase))
                    {
                        topFabric = duffleHeight + (isDoubleFoldTop ? 12 : (topLam ? 5 : 12));
                        topCut = ((length + width) * 2) + (isDoubleFoldTop ? 14 : 12);
                    }
                    else
                    {
                        topFabric = duffleHeight + (isDoubleFoldTop ? 18 : (topLam ? 5 : 12));
                        topCut = (((isInner ? length : length - 4) + (isInner ? width : width - 4)) * 2) + (isDoubleFoldTop ? 18 : 12);
                    }
                }
            }
            else
            {
                var almatis = bodyStyle.Contains("Almatis", StringComparison.OrdinalIgnoreCase);
                var circularLike = construction.Equals("Circular", StringComparison.OrdinalIgnoreCase)
                    || construction.Equals("Single Loop", StringComparison.OrdinalIgnoreCase)
                    || construction.Equals("Double Loop", StringComparison.OrdinalIgnoreCase);
                var cornerLike = construction.Equals("Buffle", StringComparison.OrdinalIgnoreCase)
                    || construction.Equals("4 Panel", StringComparison.OrdinalIgnoreCase)
                    || construction.Equals("Tube + Corner", StringComparison.OrdinalIgnoreCase)
                    || construction.Equals("Single + 4 Loop", StringComparison.OrdinalIgnoreCase)
                    || construction.Equals("Double + 4 Loop", StringComparison.OrdinalIgnoreCase);

                if (circularLike)
                {
                    topFabric = length + (isDoubleFoldTop ? 18 : 12);
                    topCut = width + (isDoubleFoldTop ? 18 : 12);
                }
                else if (cornerLike)
                {
                    if (isInner)
                    {
                        topFabric = length + (isDoubleFoldTop ? 18 : (almatis ? 15 : 12));
                        topCut = width + (isDoubleFoldTop ? 18 : (almatis ? 15 : 12));
                    }
                    else
                    {
                        topFabric = length + (isDoubleFoldTop ? (almatis ? 18 : 15) : (almatis ? 11 : 8));
                        topCut = width + (isDoubleFoldTop ? (almatis ? 18 : 15) : (almatis ? 11 : 8));
                    }

                    if (almatis)
                        factor = 4;
                }
                else
                {
                    if (isInner)
                    {
                        topFabric = length + (isDoubleFoldTop ? 18 : 12);
                        topCut = width + (isDoubleFoldTop ? 18 : 12);
                    }
                    else
                    {
                        topFabric = length + (isDoubleFoldTop ? 18 : 8);
                        topCut = width + (isDoubleFoldTop ? 18 : 8);
                    }
                }
            }

            if (topFabric <= 0 || topCut <= 0)
                return;

            var topTotalKg = Math.Round((topCut * topFabric * factor * (gsm + lami)) / 10000000, 4, MidpointRounding.AwayFromZero);
            var topTotalMtr = Math.Round((topCut / 100) * qty, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "TopFabric", topFabric);
            SetIfMissing(values, "TopCutSize", topCut);
            SetIfMissing(values, "TopTotalKg", topTotalKg);
            SetIfMissing(values, "TopTotalMtr", topTotalMtr);
        }

        private static void PopulateCalculatedSideValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "SideTotalKg")))
                return;

            var length = request.Header.SizeL ?? 0;
            var width = request.Header.SizeW ?? 0;
            var height = request.Header.SizeH ?? 0;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            var gsm = ParseDouble(GetValue(values, "SideGSM")) ?? 0;
            var lami = ParseDouble(GetValue(values, "SideLami")) ?? 0;
            if (length <= 0 || width <= 0 || height <= 0 || qty <= 0 || gsm + lami <= 0)
                return;

            var (construction, bodyStyle, bodyGrade) = ResolveBagTypeParts(request);
            var isInner = IsInnerPlacement(request);
            var isDoubleFoldBody = IsTruthy(GetValue(request.Bom3Values, "DoubleFoldBody"));
            var hasTunnel = construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase)
                && (bodyStyle.Contains("Tunnel", StringComparison.OrdinalIgnoreCase)
                    || GetValue(request.Bom3Values, "TunnelDesign").Length > 0);
            var tunnelDesign = FirstNonEmpty(GetValue(request.Bom3Values, "TunnelDesign"), GetValue(values, "TunnelDesign"));
            var isSquare = NearlyEqual(length, width);
            var conicalHeight = ParseDouble(GetValue(request.Bom3Values, "BottomConicalHeight"))
                ?? ParseDouble(GetValue(request.Bom3Values, "conicalheight"))
                ?? 0;
            var mainBottomType = (NormalizeOptional(GetValue(request.Bom3Values, "bottomtypes")) ?? "")
                .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "";

            double fabric = 0;
            double cut = 0;
            double factor = 0;
            double totalMtrFactor = 1;

            if (construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase))
            {
                if (bodyStyle.Contains("Wider Fold", StringComparison.OrdinalIgnoreCase))
                {
                    fabric = width + (isDoubleFoldBody ? (isInner ? 18 : 17) : (isInner ? 13 : 10));
                    cut = height + (isDoubleFoldBody ? (isInner ? 18 : 15) : (isInner ? 11 : 7));
                }
                else if (bodyStyle.Contains("Ventilated", StringComparison.OrdinalIgnoreCase)
                         || bodyStyle.Contains("Sulzer", StringComparison.OrdinalIgnoreCase))
                {
                    fabric = isDoubleFoldBody ? (isInner ? length + 18 : length + 8) : (isInner ? width + 4 : width);
                    cut = isDoubleFoldBody
                        ? ((ParseDouble(GetValue(values, "BodyCutSize")) ?? 0) + (isInner ? 18 : 15))
                        : height + (isInner ? 12 : 8);
                }
                else if (bodyStyle.Contains("TUNNEL CENTER JOINT", StringComparison.OrdinalIgnoreCase))
                {
                    fabric = length + 10;
                    cut = height + 10;
                    if (isDoubleFoldBody)
                    {
                        fabric = length + 18;
                        cut = height + 18;
                    }
                }
                else if (bodyStyle.Contains("Sleeve Bag", StringComparison.OrdinalIgnoreCase))
                {
                    var loopL = ParseDouble(GetValue(values, "LoopL")) ?? 0;
                    fabric = length + (isDoubleFoldBody ? 18 : 10);
                    cut = (isInner ? (ParseDouble(GetValue(values, "BodyCutSize")) ?? 0) + 18 : height + 14) + (loopL * 2);
                    if (isDoubleFoldBody)
                        cut += 18;
                }
                else if (bodyGrade.Equals("UN", StringComparison.OrdinalIgnoreCase)
                         || bodyGrade.Equals("UN+FDA", StringComparison.OrdinalIgnoreCase))
                {
                    fabric = width + (isDoubleFoldBody ? (isInner ? 18 : 15) : (isInner ? 15 : 8));
                    cut = height + (isDoubleFoldBody ? (isInner ? 18 : 15) : (isInner ? 14 : 8));
                }
                else if (hasTunnel)
                {
                    if (tunnelDesign.Contains("Greif", StringComparison.OrdinalIgnoreCase))
                    {
                        fabric = length + (isDoubleFoldBody ? 18 : 10);
                        cut = height + (isDoubleFoldBody ? 18 : 10);
                    }
                    else
                    {
                        fabric = length + (isDoubleFoldBody ? (isInner ? 18 : 15) : (isInner ? 11 : 7));
                        cut = height + (isDoubleFoldBody ? (isInner ? 18 : 15) : (isInner ? 11 : 7));
                    }
                }
                else
                {
                    fabric = width + (isDoubleFoldBody ? (isInner ? 18 : 14) : (isInner ? 11 : 7));
                    cut = height + (isDoubleFoldBody ? (isInner ? 18 : 14) : (isInner ? 11 : 7));
                }

                factor = 2;
                totalMtrFactor = 2;
            }
            else if (construction.Equals("4 Panel", StringComparison.OrdinalIgnoreCase))
            {
                if (bodyStyle.Contains("Ventilated", StringComparison.OrdinalIgnoreCase)
                    || bodyStyle.Contains("Sulzer", StringComparison.OrdinalIgnoreCase))
                {
                    fabric = width + (isDoubleFoldBody ? (isInner ? 18 : 8) : (isInner ? 4 : 0));
                    cut = height + (isDoubleFoldBody ? (isInner ? 18 : (isSquare ? 14 : 15)) : (isInner ? 11 : 7));
                }
                else
                {
                    var conicalExtra = 0d;
                    if (mainBottomType.Equals("Conical Base", StringComparison.OrdinalIgnoreCase))
                        conicalExtra = conicalHeight > 0 ? conicalHeight : width / 2;

                    fabric = width + (isDoubleFoldBody ? (isInner ? 18 : 14) : (isInner ? 11 : 7));
                    cut = height + conicalExtra + (isDoubleFoldBody ? (isInner ? 18 : 14) : (isInner ? 11 : 7));
                }

                factor = isSquare ? 4 : 2;
                totalMtrFactor = factor;
            }
            else if (construction.Equals("4 Panel + Conical Bag(Three Piece)", StringComparison.OrdinalIgnoreCase))
            {
                fabric = width + (isDoubleFoldBody ? (isInner ? 18 : 15) : (isInner ? 12 : 8));
                cut = height + (isDoubleFoldBody ? (isInner ? 18 : 15) : (isInner ? 12 : 8));
                factor = isSquare ? 4 : 2;
                totalMtrFactor = factor;
            }
            else if (construction.Equals("Double Layer Tunnel Lift Loop Bag", StringComparison.OrdinalIgnoreCase))
            {
                fabric = width + (isDoubleFoldBody ? 18 : 11);
                cut = height + (isDoubleFoldBody ? 18 : 11);
                factor = 2;
                totalMtrFactor = 2;
            }

            if (fabric <= 0 || cut <= 0 || factor <= 0)
                return;

            var totalKg = Math.Round((cut * fabric * factor * (gsm + lami)) / 10000000, 4, MidpointRounding.AwayFromZero);
            var totalMtr = Math.Round((cut / 100) * qty * totalMtrFactor, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "SideFabric", fabric);
            SetIfMissing(values, "SideCutSize", cut);
            SetIfMissing(values, "SideTotalKg", totalKg);
            SetIfMissing(values, "SideTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedTopSpoutValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "FSTotalKg")))
                return;

            var spoutType = NormalizeOptional(request.Header.FSType) ?? "";
            var topType = FirstNonEmpty(GetValue(request.Bom3Values, "toptypes"), "");
            if (!topType.Equals("Top Spout", StringComparison.OrdinalIgnoreCase)
                && !topType.Equals("Conical PlateTop", StringComparison.OrdinalIgnoreCase)
                && !topType.Equals("Conical Top", StringComparison.OrdinalIgnoreCase))
                return;

            var dia = ParseDouble(GetValue(values, "FSL")) ?? 0;
            var height = ParseDouble(GetValue(values, "FSW")) ?? 0;
            var gsm = ParseDouble(GetValue(values, "FSGSM")) ?? 0;
            var lami = ParseDouble(GetValue(values, "FSLami")) ?? 0;
            var no = ParseDouble(GetValue(request.Bom3Values, "fsno")) ?? 1;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            if (dia <= 0 || height <= 0 || gsm + lami <= 0 || qty <= 0)
                return;

            var (_, _, bodyGrade) = ResolveBagTypeParts(request);
            var isFda = bodyGrade.Equals("FDA", StringComparison.OrdinalIgnoreCase)
                || bodyGrade.Equals("UN+FDA", StringComparison.OrdinalIgnoreCase);
            var isDoubleFoldTop = IsTruthy(GetValue(request.Bom3Values, "DoubleFoldTop"));
            var topEdgeHemming = IsTruthy(GetValue(request.Bom3Values, "fsedgehaming"));
            var hasLam = lami > 0;

            double fabric = 0;
            double cut = 0;
            double weightRaw = 0;

            if (spoutType.Contains("Iris", StringComparison.OrdinalIgnoreCase)
                || spoutType.Contains("Pyjama", StringComparison.OrdinalIgnoreCase))
            {
                var iris = (dia / 2) + 12.5 + height;
                if (isDoubleFoldTop)
                {
                    fabric = isFda ? iris + 18 : iris;
                    cut = 3.14 * (dia + 18);
                }
                else
                {
                    if (isFda)
                    {
                        iris += 5;
                        fabric = iris + 3;
                    }
                    else
                    {
                        fabric = topEdgeHemming ? iris + 4 : iris;
                    }

                    cut = 3.14 * (dia + 4);
                }

                weightRaw = cut * iris * (gsm + lami) * no;
            }
            else if (spoutType.Contains("Tube", StringComparison.OrdinalIgnoreCase))
            {
                if (isDoubleFoldTop)
                {
                    cut = height + ((isFda || !hasLam) ? 18 : 12);
                    fabric = Math.Round((1.57 * dia) + 8, 0);
                }
                else
                {
                    cut = height + ((isFda || !hasLam) ? 10 : (topEdgeHemming ? 8 : 5));
                    fabric = Math.Round((1.57 * dia) + 1, 0);
                }

                weightRaw = cut * fabric * (gsm + lami) * no * 2;
            }
            else if (spoutType.Contains("Petal", StringComparison.OrdinalIgnoreCase)
                     || spoutType.Contains("Bonnet", StringComparison.OrdinalIgnoreCase))
            {
                fabric = height + (isDoubleFoldTop ? 18 : (hasLam ? (topEdgeHemming ? 8 : 5) : 12));
                cut = 3.14 * (dia + (isDoubleFoldTop ? 18 : 4));
                weightRaw = cut * fabric * (gsm + lami) * no;
            }
            else
            {
                if (isDoubleFoldTop)
                {
                    fabric = height + (isFda ? 18 : (hasLam ? 12 : 18));
                    cut = 3.14 * (dia + 11);
                }
                else
                {
                    fabric = height + (isFda ? 10 : (hasLam ? (topEdgeHemming ? 8 : 5) : 12));
                    cut = 3.14 * (dia + 4);
                }

                weightRaw = cut * fabric * (gsm + lami) * no;
            }

            if (fabric <= 0 || cut <= 0 || weightRaw <= 0)
                return;

            var totalKg = Math.Round(weightRaw / 10000000, 4, MidpointRounding.AwayFromZero);
            var totalMtr = Math.Round((cut / 100) * qty * no, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "FSFabric", fabric);
            SetIfMissing(values, "FSCutSize", cut);
            SetIfMissing(values, "FSTotalKg", totalKg);
            SetIfMissing(values, "FSTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedBottomValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "BottomTotalKg")))
                return;

            var bottomType = NormalizeOptional(GetValue(request.Bom3Values, "bottomtypes")) ?? "";
            var mainBottomType = bottomType.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Flat";
            var length = request.Header.SizeL ?? 0;
            var width = request.Header.SizeW ?? 0;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            var gsm = ParseDouble(GetValue(values, "BottomGSM")) ?? 0;
            var lami = ParseDouble(GetValue(values, "BottomLami")) ?? 0;
            var bottomNo = ParseDouble(GetValue(values, "bottomno")) ?? 1;
            if (length <= 0 || width <= 0 || qty <= 0 || gsm + lami <= 0)
                return;

            var (construction, bodyStyle, _) = ResolveBagTypeParts(request);
            var isInner = IsInnerPlacement(request);
            var isDoubleFoldBottom = IsTruthy(GetValue(request.Bom3Values, "DoubleFoldBottom"));
            var conicalHeight = ParseDouble(GetValue(request.Bom3Values, "BottomConicalHeight"))
                ?? ParseDouble(GetValue(request.Bom3Values, "conicalheight"))
                ?? 0;
            var bottomDia = ParseDouble(GetValue(values, "DSL")) ?? 0;

            double fabric = 0;
            double cut = 0;
            double factor = 1;

            if (mainBottomType.Equals("Conical Plate Base", StringComparison.OrdinalIgnoreCase) && conicalHeight > 0)
            {
                fabric = length + (isDoubleFoldBottom ? 18 : (isInner ? conicalHeight : conicalHeight - 4));
                cut = width + (isDoubleFoldBottom ? 18 : (isInner ? conicalHeight : conicalHeight - 4));
            }
            else if (mainBottomType.Equals("Conical Base", StringComparison.OrdinalIgnoreCase) && bottomDia > 0)
            {
                var oneSideDia = (bottomDia * 3.14) / 4;
                fabric = length + (isInner ? (isDoubleFoldBottom ? 18 : 12) : (isDoubleFoldBottom ? 15 : 8));
                cut = ((width - oneSideDia) / 2) + (isInner ? (isDoubleFoldBottom ? 18 : 14) : (isDoubleFoldBottom ? 18 : 10));
                factor = 4;
            }
            else if (construction.Equals("Circular", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Single Loop", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Double Loop", StringComparison.OrdinalIgnoreCase))
            {
                if ((construction.Equals("Single Loop", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Double Loop", StringComparison.OrdinalIgnoreCase))
                    && mainBottomType.Equals("Star Bottom", StringComparison.OrdinalIgnoreCase))
                    return;

                fabric = length + (isDoubleFoldBottom ? 18 : (construction.Equals("Circular", StringComparison.OrdinalIgnoreCase) ? 12 : 10));
                cut = width + (isDoubleFoldBottom ? 18 : (construction.Equals("Circular", StringComparison.OrdinalIgnoreCase) ? 12 : 10));
            }
            else if (construction.Equals("4 Panel", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Buffle", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Tube + Corner", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Single + 4 Loop", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Double + 4 Loop", StringComparison.OrdinalIgnoreCase))
            {
                var almatis = bodyStyle.Contains("Almatis", StringComparison.OrdinalIgnoreCase);
                if (isInner)
                {
                    fabric = length + (isDoubleFoldBottom ? 18 : 12);
                    cut = width + (isDoubleFoldBottom ? 18 : 12);
                }
                else
                {
                    fabric = length + (isDoubleFoldBottom ? 15 : 8);
                    cut = width + (isDoubleFoldBottom ? 15 : 8);
                }

                if (almatis)
                    factor = 4;
            }
            else if (construction.Equals("Double Layer Tunnel Lift Loop Bag", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Double Layer Circular Inner Skin Bag", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Hood Bag-Covered Bag", StringComparison.OrdinalIgnoreCase))
            {
                fabric = length + (isInner ? (isDoubleFoldBottom ? 18 : 12) : (isDoubleFoldBottom ? 15 : 8));
                cut = width + (isInner ? (isDoubleFoldBottom ? 18 : 12) : (isDoubleFoldBottom ? 15 : 8));
            }

            if (fabric <= 0 || cut <= 0)
                return;

            var totalKg = Math.Round(((cut * fabric * factor * (gsm + lami)) * bottomNo) / 10000000, 4, MidpointRounding.AwayFromZero);
            var totalMtr = Math.Round((cut / 100) * qty, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "BottomFabric", fabric);
            SetIfMissing(values, "BottomCutSize", cut);
            SetIfMissing(values, "BottomTotalKg", totalKg);
            SetIfMissing(values, "BottomTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedBottomSpoutValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "DSTotalKg")))
                return;

            var bottomType = NormalizeOptional(GetValue(request.Bom3Values, "bottomtypes")) ?? NormalizeOptional(request.Header.DSType) ?? "";
            var parts = bottomType.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var mainType = parts.ElementAtOrDefault(0) ?? "";
            var subType = parts.ElementAtOrDefault(1) ?? "";
            if (!mainType.Equals("Bottom Spout", StringComparison.OrdinalIgnoreCase))
                return;

            var dia = ParseDouble(GetValue(values, "DSL")) ?? 0;
            var height = ParseDouble(GetValue(values, "DSW")) ?? 0;
            var gsm = ParseDouble(GetValue(values, "DSGSM")) ?? 0;
            var lami = ParseDouble(GetValue(values, "DSLami")) ?? 0;
            var no = ParseDouble(GetValue(request.Bom3Values, "dsno")) ?? 1;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            if (dia <= 0 || height <= 0 || gsm + lami <= 0 || qty <= 0)
                return;

            var (_, _, bodyGrade) = ResolveBagTypeParts(request);
            var isFda = bodyGrade.Equals("FDA", StringComparison.OrdinalIgnoreCase)
                || bodyGrade.Equals("UN+FDA", StringComparison.OrdinalIgnoreCase);
            var isDoubleFoldBottom = IsTruthy(GetValue(request.Bom3Values, "DoubleFoldBottom"))
                || IsTruthy(GetValue(request.Bom3Values, "DoubleFoldBottomSpout"));
            var bottomEdgeHemming = IsTruthy(GetValue(request.Bom3Values, "dsedgehaming"));
            var hasFillerCord = IsTruthy(GetValue(request.Bom3Values, "fillercordbottom"))
                || IsTruthy(GetValue(request.Bom3Values, "fillercord"))
                || IsTruthy(GetValue(request.Bom3Values, "isfillercord"));
            var hasLam = lami > 0;

            double fabric = 0;
            double cut = 0;
            double weightRaw = 0;

            if (subType.Contains("Tube", StringComparison.OrdinalIgnoreCase))
            {
                cut = height + (isDoubleFoldBottom ? ((isFda || !hasLam) ? 18 : 12) : ((isFda || !hasLam) ? 10 : 5));
                fabric = Math.Round((1.57 * dia) + (isDoubleFoldBottom ? 8 : 1), 0);
                weightRaw = cut * fabric * (gsm + lami) * no * 2;
            }
            else if (subType.Contains("Iris", StringComparison.OrdinalIgnoreCase)
                     || subType.Contains("Pyjama", StringComparison.OrdinalIgnoreCase)
                     || subType.Contains("Bonnet", StringComparison.OrdinalIgnoreCase))
            {
                var iris = (dia / 2) + 12.5 + height;
                if (isDoubleFoldBottom)
                {
                    fabric = isFda ? iris + 12 : iris + 18;
                    cut = 3.14 * (dia + 12);
                }
                else
                {
                    fabric = isFda ? iris + 5 : (bottomEdgeHemming ? iris + 4 : iris);
                    cut = 3.14 * (dia + 4);
                }

                weightRaw = cut * iris * (gsm + lami) * no;
            }
            else
            {
                if (isDoubleFoldBottom)
                {
                    if (isFda)
                        fabric = height + 20;
                    else
                        fabric = height + (hasFillerCord ? 17 : 12);
                    cut = 3.14 * (dia + 11);
                }
                else
                {
                    if (isFda)
                        fabric = height + 13;
                    else if (hasFillerCord)
                        fabric = height + 10;
                    else if (bottomEdgeHemming)
                        fabric = height + 8;
                    else
                        fabric = height + 5;
                    cut = 3.14 * (dia + 4);
                }

                weightRaw = cut * fabric * (gsm + lami) * no;
            }

            if (fabric <= 0 || cut <= 0 || weightRaw <= 0)
                return;

            var totalKg = Math.Round(weightRaw / 10000000, 4, MidpointRounding.AwayFromZero);
            var totalMtr = Math.Round((cut / 100) * qty * no, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "DSFabric", fabric);
            SetIfMissing(values, "DSCutSize", cut);
            SetIfMissing(values, "DSTotalKg", totalKg);
            SetIfMissing(values, "DSTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedTopSpoutTieValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "FSTieTotalKg")))
                return;

            var grm = ParseDouble(GetValue(values, "FSTieGSM")) ?? 0;
            var size = ParseDouble(GetValue(values, "FSTieFabric")) ?? 0;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            var no = ParseDouble(GetValue(request.Bom3Values, "TopSpoutTieNo")) ?? 0;
            if (grm <= 0 || qty <= 0 || no <= 0)
                return;

            var cut = ResolveTieCutLength(GetValue(values, "FSTieRemarks"), GetValue(values, "FSTieCutSize"));
            if (cut <= 0)
                return;

            var totalKg = Math.Round((cut * grm * no) / 100000, 4, MidpointRounding.AwayFromZero);
            var totalMtr = Math.Round((cut / 100) * qty * no, 4, MidpointRounding.AwayFromZero);

            if (size > 0)
                SetIfMissing(values, "FSTieFabric", size);
            SetIfMissing(values, "FSTieCutSize", cut);
            SetIfMissing(values, "FSTieTotalKg", totalKg);
            SetIfMissing(values, "FSTieTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedBottomSpoutTieValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "DSTieTotalKg")))
                return;

            var grm = ParseDouble(GetValue(values, "DSTieGSM")) ?? 0;
            var size = ParseDouble(GetValue(values, "DSTieFabric")) ?? 0;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            var no = ParseDouble(GetValue(request.Bom3Values, "BottomSpoutTieNo")) ?? 0;
            if (grm <= 0 || qty <= 0 || no <= 0)
                return;

            var cut = ResolveTieCutLength(GetValue(values, "DSTieRemarks"), GetValue(values, "DSTieCutSize"));
            if (cut <= 0)
                return;

            var totalKg = Math.Round((cut * grm * no) / 100000, 4, MidpointRounding.AwayFromZero);
            var totalMtr = Math.Round((cut / 100) * qty * no, 4, MidpointRounding.AwayFromZero);

            if (size > 0)
                SetIfMissing(values, "DSTieFabric", size);
            SetIfMissing(values, "DSTieCutSize", cut);
            SetIfMissing(values, "DSTieTotalKg", totalKg);
            SetIfMissing(values, "DSTieTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedTopSpoutIrisTieValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "FSIRISTieTotalKg")))
                return;

            var grm = ParseDouble(FirstNonEmpty(GetValue(values, "FSIRISTieGSM"), GetValue(values, "FSTieGSM"))) ?? 0;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            var no = ParseDouble(GetValue(request.Bom3Values, "TopSpoutTieIRISNo")) ?? 0;
            var dia = ParseDouble(GetValue(values, "FSL")) ?? 0;
            if (grm <= 0 || qty <= 0 || no <= 0 || dia <= 0)
                return;

            var cut = Math.Round((dia * 3.14d) + 35d, 4, MidpointRounding.AwayFromZero);
            var totalKg = Math.Round((cut * grm * no) / 100000, 4, MidpointRounding.AwayFromZero);
            var totalMtr = Math.Round((cut / 100) * qty * no, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "FSIRISTieCutSize", cut);
            SetIfMissing(values, "FSIRISTieTotalKg", totalKg);
            SetIfMissing(values, "FSIRISTieTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedBottomSpoutIrisTieValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "DSIRISTieTotalKg")))
                return;

            var grm = ParseDouble(FirstNonEmpty(GetValue(values, "DSIRISTieGSM"), GetValue(values, "DSTieGSM"))) ?? 0;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            var no = ParseDouble(GetValue(request.Bom3Values, "BottomSpoutTieIRISNo")) ?? 0;
            var dia = ParseDouble(GetValue(values, "DSL")) ?? 0;
            if (grm <= 0 || qty <= 0 || no <= 0 || dia <= 0)
                return;

            var cut = Math.Round((dia * 3.14d) + 35d, 4, MidpointRounding.AwayFromZero);
            var totalKg = Math.Round((cut * grm * no) / 100000, 4, MidpointRounding.AwayFromZero);
            var totalMtr = Math.Round((cut / 100) * qty * no, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "DSIRISTieCutSize", cut);
            SetIfMissing(values, "DSIRISTieTotalKg", totalKg);
            SetIfMissing(values, "DSIRISTieTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedLinerValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "LinerTotalKg")))
                return;

            var qty = ParseDouble(request.Header.Qty) ?? 0;
            var length = request.Header.SizeL ?? 0;
            var width = request.Header.SizeW ?? 0;
            var height = request.Header.SizeH ?? 0;
            var micron = ParseDouble(GetValue(values, "LinerDim")) ?? 0;
            if (qty <= 0 || micron <= 0 || length <= 0 || width <= 0 || height <= 0)
                return;

            var explicitCut = ParseDouble(GetValue(values, "LinerL")) ?? 0;
            var explicitFabric = ParseDouble(GetValue(values, "LinerW")) ?? 0;
            var linerMaterial = NormalizeOptional(GetValue(values, "Liner")) ?? "";
            var linerType = (NormalizeOptional(GetValue(values, "LinerType")) ?? "").Split('|', StringSplitOptions.TrimEntries)[0];
            var topType = NormalizeOptional(GetValue(request.Bom3Values, "toptypes")) ?? "Open";
            var bottomType = NormalizeOptional(GetValue(request.Bom3Values, "bottomtypes")) ?? "Flat";
            var mainBottomType = bottomType.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Flat";
            var isInner = IsInnerPlacement(request);
            var duffleHeight = ParseDouble(GetValue(values, "DuffleHt")) ?? 0;
            var slitHt = ParseDouble(GetValue(values, "SlitHt")) ?? 0;
            var topSpoutHeight = ParseDouble(GetValue(values, "FSW")) ?? 0;
            var topSpoutDia = ParseDouble(GetValue(values, "FSL")) ?? 0;
            var bottomSpoutHeight = ParseDouble(GetValue(values, "DSW")) ?? 0;
            var bottomSpoutDia = ParseDouble(GetValue(values, "DSL")) ?? 0;

            var density = ResolveLinerDensity(linerMaterial);
            double fabric = 0;
            double cut = 0;

            if (explicitFabric > 0)
            {
                fabric = explicitFabric;
                cut = explicitCut;
            }
            else
            {
                var adjustedLength = isInner ? length : Math.Max(0, length - 4);
                var adjustedWidth = isInner ? width : Math.Max(0, width - 4);
                var bottom = ((length / 2) + (width / 2)) / 2;
                var computedBottomSpoutDia = bottomSpoutHeight > 0
                    ? Math.Round(((((adjustedLength + adjustedWidth + 5) - (1.57 * bottomSpoutDia + 3)) / 4) * 1.12), 0)
                    : 0;
                var computedTopSpoutDia = topSpoutHeight > 0
                    ? Math.Round(((((adjustedLength + adjustedWidth + 5) - (1.57 * topSpoutDia + 3)) / 4) * 1.12), 0)
                    : 0;

                var addCm = isInner ? 10 : 15;
                if (isInner && bottomSpoutHeight > 0)
                    addCm += 5;
                if (isInner && topSpoutHeight > 0)
                    addCm += 5;

                if (linerType.Equals("Form Fit Liner", StringComparison.OrdinalIgnoreCase)
                    || linerType.Equals("Form Fit Flenze Liner", StringComparison.OrdinalIgnoreCase))
                {
                    fabric = adjustedLength + adjustedWidth + (linerType.Contains("Flenze", StringComparison.OrdinalIgnoreCase) ? 25 : 5);

                    if (mainBottomType.Equals("Bottom Spout", StringComparison.OrdinalIgnoreCase)
                        && (topType.Equals("Open", StringComparison.OrdinalIgnoreCase)
                            || topType.Equals("Duffle or Skrit", StringComparison.OrdinalIgnoreCase)
                            || topType.Equals("Top + Skrit", StringComparison.OrdinalIgnoreCase)
                            || topType.Equals("Leno", StringComparison.OrdinalIgnoreCase)))
                    {
                        cut = topType.Equals("Open", StringComparison.OrdinalIgnoreCase)
                            ? ((adjustedLength + adjustedWidth) / 2) - 10 + computedBottomSpoutDia + height + 10 + bottomSpoutHeight + 5
                            : duffleHeight + 5 + computedBottomSpoutDia + height + bottomSpoutHeight + addCm;
                    }
                    else
                    {
                        cut = topSpoutHeight + computedTopSpoutDia + computedBottomSpoutDia + height + bottomSpoutHeight + addCm;
                        if (topType.Equals("Open", StringComparison.OrdinalIgnoreCase)
                            || mainBottomType.Equals("Flat", StringComparison.OrdinalIgnoreCase))
                            cut += (((adjustedLength + adjustedWidth) / 2) / 2) + 5;
                    }
                }
                else if (linerType.Equals("Gusseted Liner", StringComparison.OrdinalIgnoreCase))
                {
                    fabric = adjustedLength + adjustedWidth + 5;

                    if ((topType.Equals("Open", StringComparison.OrdinalIgnoreCase)
                         || topType.Equals("Duffle or Skrit", StringComparison.OrdinalIgnoreCase)
                         || topType.Equals("Top + Skrit", StringComparison.OrdinalIgnoreCase)
                         || topType.Equals("Leno", StringComparison.OrdinalIgnoreCase))
                        && mainBottomType.Equals("Flat", StringComparison.OrdinalIgnoreCase))
                    {
                        cut = bottom + height + 80 + addCm;
                    }
                    else if ((topType.Equals("Duffle or Skrit", StringComparison.OrdinalIgnoreCase)
                              || topType.Equals("Top + Skrit", StringComparison.OrdinalIgnoreCase)
                              || topType.Equals("Leno", StringComparison.OrdinalIgnoreCase))
                             && mainBottomType.Equals("Bottom Spout", StringComparison.OrdinalIgnoreCase))
                    {
                        cut = bottom + height + duffleHeight + bottomSpoutHeight + addCm;
                    }
                    else if (topType.Equals("Top Spout", StringComparison.OrdinalIgnoreCase)
                             && mainBottomType.Equals("Flat", StringComparison.OrdinalIgnoreCase))
                    {
                        cut = ((adjustedWidth + adjustedLength) / 2) + height + topSpoutHeight + addCm;
                    }
                    else if (topType.Equals("Top Spout", StringComparison.OrdinalIgnoreCase)
                             && mainBottomType.Equals("Bottom Spout", StringComparison.OrdinalIgnoreCase))
                    {
                        cut = ((adjustedWidth + adjustedLength) / 2) + height + topSpoutHeight + bottomSpoutHeight + 20;
                    }
                    else if (topType.Equals("Open", StringComparison.OrdinalIgnoreCase)
                             && mainBottomType.Equals("Bottom Spout", StringComparison.OrdinalIgnoreCase))
                    {
                        cut = bottom + height + bottomSpoutHeight + 100;
                    }
                }
                else if (linerType.Equals("Suspended", StringComparison.OrdinalIgnoreCase) && slitHt > 0)
                {
                    fabric = adjustedLength + adjustedWidth + 5;
                    cut = height + slitHt + adjustedWidth + 10;
                }
                else if (linerType.Equals("Tray Liner", StringComparison.OrdinalIgnoreCase) && explicitCut > 0)
                {
                    fabric = isInner ? (length * 2) + (width * 2) + 10 : length + width;
                    cut = explicitCut + (width / 2) + 5;
                }
            }

            if (fabric <= 0 || cut <= 0)
                return;

            var totalKg = Math.Round((cut * fabric * 2 * micron * density) / 10000000, 4, MidpointRounding.AwayFromZero);
            var totalMtr = Math.Round((cut / 100) * qty, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "LinerFabric", fabric);
            SetIfMissing(values, "LinerCutSize", cut);
            SetIfMissing(values, "LinerTotalKg", totalKg);
            SetIfMissing(values, "LinerTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedDocValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "DocTotalKg")))
                return;

            var length = ParseDouble(GetValue(values, "docl")) ?? 0;
            var width = ParseDouble(GetValue(values, "docw")) ?? 0;
            var micron = ParseDouble(GetValue(values, "DocGSM")) ?? 100;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            if (length <= 0 || width <= 0 || qty <= 0)
                return;

            var docParts = (request.Header.Doc ?? "")
                .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var docType = docParts.ElementAtOrDefault(0) ?? "";
            var openType = docParts.ElementAtOrDefault(1) ?? "";
            var count = ParseDouble(request.Header.DocNumber)
                ?? ParseDouble(docParts.ElementAtOrDefault(3))
                ?? 1;
            var isInch = string.Equals(request.Header.DocUnit, "INCH", StringComparison.OrdinalIgnoreCase);

            double fabric = length;
            double cut = width;

            var horizontalOpen = openType.Contains("RHS Open", StringComparison.OrdinalIgnoreCase)
                || openType.Contains("Upside Open", StringComparison.OrdinalIgnoreCase)
                || openType.Contains("Horizontal Open", StringComparison.OrdinalIgnoreCase);

            if (horizontalOpen)
                fabric += 4;
            else
                cut += 4;

            if (isInch)
            {
                fabric *= 2.54;
                cut *= 2.54;
            }

            var totalKg = fabric * cut * 2 * micron * 0.92d;
            if (docType.Contains("Zip Lock", StringComparison.OrdinalIgnoreCase))
                totalKg += totalKg * 0.16d;
            totalKg = Math.Round((totalKg * count) / 10000000, 4, MidpointRounding.AwayFromZero);

            var totalMtr = Math.Round((cut / 100) * qty, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "DocFabric", fabric);
            SetIfMissing(values, "DocCutSize", cut);
            SetIfMissing(values, "DocTotalKg", totalKg);
            SetIfMissing(values, "DocTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedLabelValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "LableTotalKg")))
                return;

            var length = ParseDouble(GetValue(values, "LabelL")) ?? ParseDouble(GetValue(values, "LableFabric")) ?? 0;
            var width = ParseDouble(GetValue(values, "LabelW")) ?? ParseDouble(GetValue(values, "LableCutSize")) ?? 0;
            var micron = ParseDouble(GetValue(values, "LableGSM")) ?? 100;
            var qty = ParseDouble(request.Header.Qty) ?? 0;
            if (length <= 0 || width <= 0 || qty <= 0)
                return;

            var isTyvac = IsTruthy(GetValue(values, "IsTyvac")) || GetValue(values, "LabelType").Contains("Tyvac", StringComparison.OrdinalIgnoreCase);
            var density = isTyvac ? 1d : 0.92d;
            var totalKg = Math.Round((length * width * micron * 2.54d * 2.54d * density) / 10000000, 4, MidpointRounding.AwayFromZero);
            var totalMtr = Math.Round((width / 100) * qty, 4, MidpointRounding.AwayFromZero);

            SetIfMissing(values, "LableFabric", length);
            SetIfMissing(values, "LableCutSize", width);
            SetIfMissing(values, "LableTotalKg", totalKg);
            SetIfMissing(values, "LableTotalMtr", totalMtr);
        }

        private static void PopulateCalculatedFillerCordValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "FillerCordTotalKg")))
                return;

            var gpm = ParseDouble(GetValue(values, "FillerCordGSM")) ?? 0;
            var length = request.Header.SizeL ?? 0;
            var width = request.Header.SizeW ?? 0;
            var height = request.Header.SizeH ?? 0;
            if (gpm <= 0 || length <= 0 || width <= 0 || height <= 0)
                return;

            var (construction, _, _) = ResolveBagTypeParts(request);
            var isInner = IsInnerPlacement(request);
            var addCm = isInner ? 10 : 0;
            var totalLengthCm = 0d;

            if (IsTruthy(GetValue(request.Bom3Values, "fillercordtop")))
                totalLengthCm += ResolveSingleDoubleMultiplier(GetValue(request.Bom3Values, "fillercordtoptype")) * ((length + width + addCm) * 2);

            if (IsTruthy(GetValue(request.Bom3Values, "fillercordbottom")))
            {
                var baseLength = construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase)
                    ? (width + addCm) * 2
                    : (length + width + addCm) * 2;
                totalLengthCm += ResolveSingleDoubleMultiplier(GetValue(request.Bom3Values, "fillercordbottomtype")) * baseLength;
            }

            var topSpoutDia = ParseDouble(GetValue(values, "FSL")) ?? 0;
            var topSpoutHeight = ParseDouble(GetValue(values, "FSW")) ?? 0;
            if (IsTruthy(GetValue(request.Bom3Values, "fillercordtopspout")) && topSpoutDia > 0 && topSpoutHeight > 0)
                totalLengthCm += ResolveSingleDoubleMultiplier(GetValue(request.Bom3Values, "fillercordFStype"))
                    * ((3.14 * topSpoutDia) + 12 + topSpoutHeight);

            var bottomSpoutDia = ParseDouble(GetValue(values, "DSL")) ?? 0;
            var bottomSpoutHeight = ParseDouble(GetValue(values, "DSW")) ?? 0;
            if (IsTruthy(GetValue(request.Bom3Values, "fillercordbottomspout")) && bottomSpoutDia > 0 && bottomSpoutHeight > 0)
                totalLengthCm += ResolveSingleDoubleMultiplier(GetValue(request.Bom3Values, "fillercordDStype"))
                    * ((3.14 * bottomSpoutDia) + 12 + bottomSpoutHeight);

            var circularLike = construction.Equals("Circular", StringComparison.OrdinalIgnoreCase)
                || construction.Equals("Single Loop", StringComparison.OrdinalIgnoreCase)
                || construction.Equals("Double Loop", StringComparison.OrdinalIgnoreCase);
            if (IsTruthy(GetValue(request.Bom3Values, "fillercordbody")) && !circularLike)
            {
                var baseHeight = isInner ? height + addCm : height;
                totalLengthCm += ResolveSingleDoubleMultiplier(GetValue(request.Bom3Values, "fillercordbodytype")) * (baseHeight * 4);
            }

            if (construction.Equals("Buffle", StringComparison.OrdinalIgnoreCase))
            {
                var baseHeight = isInner ? height + 5 : height;
                totalLengthCm += ResolveSingleDoubleMultiplier(GetValue(request.Bom3Values, "fillercordbuffletype")) * (baseHeight * 8);
            }

            if (totalLengthCm <= 0)
                return;

            var totalKg = Math.Round((totalLengthCm * gpm) / 100000, 4, MidpointRounding.AwayFromZero);
            SetIfMissing(values, "FillerCordTotalKg", totalKg);
        }

        private static void PopulateCalculatedThreadValues(BomCreateRequest request)
        {
            var values = request.Bom1Values;
            if (!string.IsNullOrWhiteSpace(GetValue(values, "ThreadTotalKg")))
                return;

            var length = request.Header.SizeL ?? 0;
            var width = request.Header.SizeW ?? 0;
            var height = request.Header.SizeH ?? 0;
            if (length <= 0 || width <= 0 || height <= 0)
                return;

            var (construction, bodyStyle, _) = ResolveBagTypeParts(request);
            var isInner = IsInnerPlacement(request);
            var topType = NormalizeOptional(GetValue(request.Bom3Values, "toptypes")) ?? "Open";
            var bottomType = NormalizeOptional(GetValue(request.Bom3Values, "bottomtypes")) ?? "Flat";
            var mainBottomType = bottomType.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Flat";
            var loopConst = GetValue(values, "loopconst");
            var swl = ParseDouble(request.Header.Swl) ?? 0;
            var duffleHeight = ParseDouble(GetValue(values, "DuffleHt")) ?? 0;
            var topSpoutDia = ParseDouble(GetValue(values, "FSL")) ?? 0;
            var topSpoutHeight = ParseDouble(GetValue(values, "FSW")) ?? 0;
            var bottomSpoutDia = ParseDouble(GetValue(values, "DSL")) ?? 0;
            var bottomSpoutHeight = ParseDouble(GetValue(values, "DSW")) ?? 0;

            var threadLengthCm = 0d;

            var hasTop = topType.Length > 0 && !topType.Equals("Open", StringComparison.OrdinalIgnoreCase);
            var hasBottom = mainBottomType.Length > 0 && !mainBottomType.Equals("Flat", StringComparison.OrdinalIgnoreCase);

            if (isInner)
            {
                if (hasTop)
                    threadLengthCm += (length + width + 10) * 2;
                if (hasBottom && !construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase))
                    threadLengthCm += (length + width + 10) * 2;
            }
            else
            {
                if (hasTop)
                    threadLengthCm += (length + width) * 2;
                if (hasBottom && !construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase))
                    threadLengthCm += (length + width) * 2;
            }

            if (topType.Equals("Top Spout", StringComparison.OrdinalIgnoreCase) && topSpoutDia > 0 && topSpoutHeight > 0)
                threadLengthCm += ((3.14 * topSpoutDia) + 12 + topSpoutHeight + 10) * 2;

            if ((topType.Equals("Duffle or Skrit", StringComparison.OrdinalIgnoreCase)
                 || topType.Equals("Top + Skrit", StringComparison.OrdinalIgnoreCase)
                 || topType.Equals("Leno", StringComparison.OrdinalIgnoreCase)
                 || topType.Equals("Oversize Duffle or Skrit", StringComparison.OrdinalIgnoreCase)
                 || topType.Equals("Drawstring Skirt", StringComparison.OrdinalIgnoreCase)
                 || topType.Equals("Jute Skirt", StringComparison.OrdinalIgnoreCase))
                && duffleHeight > 0)
            {
                threadLengthCm += (isInner ? duffleHeight + 5 : duffleHeight) * 2;
            }

            if (mainBottomType.Equals("Bottom Spout", StringComparison.OrdinalIgnoreCase) && bottomSpoutDia > 0 && bottomSpoutHeight > 0)
                threadLengthCm += ((3.14 * bottomSpoutDia) + 12 + 10 + bottomSpoutHeight) * 2;

            var hasTunnel = construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase)
                && (bodyStyle.Contains("Tunnel", StringComparison.OrdinalIgnoreCase)
                    || GetValue(request.Bom3Values, "TunnelDesign").Length > 0);

            if (construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase))
            {
                if (hasTunnel)
                {
                    threadLengthCm += isInner ? (((height * 2) + length + 10) * 2) + 150 : (((height * 2) + length) * 2) + 140;
                    threadLengthCm += width * 4;
                }
                else
                {
                    threadLengthCm += isInner ? (((height * 2) + length + 10) * 2) : (((height * 2) + length) * 2);
                }

                threadLengthCm += isInner ? (height + 5) * 4 : height * 4;
            }
            else if (construction.Equals("Buffle", StringComparison.OrdinalIgnoreCase))
            {
                var seamFactor = ResolveThreadBuffleFactor(GetValue(request.Bom3Values, "ThreadBuffleSeam"));
                if (seamFactor > 0)
                    threadLengthCm += (isInner ? height + 5 : height) * seamFactor;
            }
            else if (construction.Equals("4 Panel", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Tube + Corner", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Single + 4 Loop", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("Double + 4 Loop", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("4 Panel + Conical Bag(Three Piece)", StringComparison.OrdinalIgnoreCase)
                     || construction.Equals("4 Panel + Conical Bag(Single Piece)", StringComparison.OrdinalIgnoreCase))
            {
                threadLengthCm += (isInner ? height + 5 : height) * 8;
            }

            if (loopConst.Equals("Cross Corner", StringComparison.OrdinalIgnoreCase))
                threadLengthCm += swl > 1250 ? 2500 : (swl >= 500 ? 2000 : 0);

            if (IsTruthy(GetValue(request.Bom3Values, "Hiracle")))
            {
                if (IsTruthy(GetValue(request.Bom3Values, "HiracleTop")))
                    threadLengthCm += (isInner ? (length + width + 10) * 2 : (length + width) * 2) * 4;

                if (IsTruthy(GetValue(request.Bom3Values, "HIracleBottom")))
                {
                    if (construction.Equals("Buffle", StringComparison.OrdinalIgnoreCase))
                        threadLengthCm += (isInner ? (length + width + 10) * 2 : (length + width) * 2) * 4;
                    else
                        threadLengthCm += (isInner ? ((length * 2) + 10) : (length * 2)) * 4;
                }

                if (construction.Equals("UPanel", StringComparison.OrdinalIgnoreCase))
                {
                    if (hasTunnel)
                        threadLengthCm += isInner ? (((height + 5) * 2 + length + 5) * 2 * 4) : (((height * 2) + length) * 2 * 4);
                    else
                        threadLengthCm += isInner ? ((height + 10) * 16) : (height * 16);
                }
                else if (construction.Equals("Buffle", StringComparison.OrdinalIgnoreCase))
                {
                    threadLengthCm += (isInner ? height + 5 : height) * 16;
                }
                else if (construction.Equals("4 Panel", StringComparison.OrdinalIgnoreCase)
                         || construction.Equals("Tube + Corner", StringComparison.OrdinalIgnoreCase)
                         || construction.Equals("Single + 4 Loop", StringComparison.OrdinalIgnoreCase)
                         || construction.Equals("Double + 4 Loop", StringComparison.OrdinalIgnoreCase)
                         || construction.Equals("4 Panel + Conical Bag(Three Piece)", StringComparison.OrdinalIgnoreCase)
                         || construction.Equals("4 Panel + Conical Bag(Single Piece)", StringComparison.OrdinalIgnoreCase))
                {
                    threadLengthCm += (isInner ? height + 5 : height) * 16;
                }
            }

            if (threadLengthCm <= 0)
                return;

            var totalKg = Math.Round(threadLengthCm / 100000, 4, MidpointRounding.AwayFromZero);
            SetIfMissing(values, "ThreadTotalKg", totalKg);
        }

        private static List<BomCreateLineInput> BuildDerivedLines(BomCreateRequest request)
        {
            var lines = new List<BomCreateLineInput>();
            var values = request.Bom1Values;
            var editor = request.Bom3Values;
            var sortOrder = 1;

            AddDerivedLine(lines, ref sortOrder, "Body",
                GetValue(values, "BodyGSM"),
                GetValue(values, "BodyLami"),
                FirstNonEmpty(GetValue(values, "BodyColor"), request.Header.FabColor),
                GetValue(values, "BodyFabric"),
                GetValue(values, "BodyCutSize"),
                ParseDouble(GetValue(values, "BodyTotalMtr")),
                ParseDouble(GetValue(values, "BodyTotalKg")),
                FirstNonEmpty(GetValue(values, "BodyRemarks"), request.Header.BodyRemarks));

            AddDerivedLine(lines, ref sortOrder, "Side",
                GetValue(values, "SideGSM"),
                GetValue(values, "SideLami"),
                FirstNonEmpty(GetValue(values, "SideColor"), request.Header.FabColor),
                GetValue(values, "SideFabric"),
                GetValue(values, "SideCutSize"),
                ParseDouble(GetValue(values, "SideTotalMtr")),
                ParseDouble(GetValue(values, "SideTotalKg")),
                GetValue(values, "SideRemarks"));

            AddDerivedLine(lines, ref sortOrder, "Top",
                GetValue(values, "TopGSM"),
                GetValue(values, "TopLami"),
                FirstNonEmpty(GetValue(values, "TopColor"), request.Header.FabColor),
                GetValue(values, "TopFabric"),
                GetValue(values, "TopCutSize"),
                ParseDouble(GetValue(values, "TopTotalMtr")),
                ParseDouble(GetValue(values, "TopTotalKg")),
                FirstNonEmpty(GetValue(values, "TopRemarks"), GetValue(values, "TopRemarks1")));

            AddDerivedLine(lines, ref sortOrder, "Top Spout",
                GetValue(values, "FSGSM"),
                GetValue(values, "FSLami"),
                GetValue(values, "FSColor"),
                GetValue(values, "FSFabric"),
                GetValue(values, "FSCutSize"),
                ParseDouble(GetValue(values, "FSTotalMtr")),
                ParseDouble(GetValue(values, "FSTotalKg")),
                FirstNonEmpty(GetValue(values, "FSRemarks"), request.Header.FSType));

            AddDerivedLine(lines, ref sortOrder, "Top Spout Tie",
                GetValue(values, "FSTieGSM"),
                GetValue(values, "FSTieLami"),
                GetValue(values, "FSTieColor"),
                GetValue(values, "FSTieFabric"),
                GetValue(values, "FSTieCutSize"),
                ParseDouble(GetValue(values, "FSTieTotalMtr")),
                ParseDouble(GetValue(values, "FSTieTotalKg")),
                FirstNonEmpty(GetValue(values, "FSTieRemarks"), GetValue(values, "FSTieRemarks1")));

            AddDerivedLine(lines, ref sortOrder, "Bottom",
                GetValue(values, "BottomGSM"),
                GetValue(values, "BottomLami"),
                FirstNonEmpty(GetValue(values, "BottomColor"), request.Header.FabColor),
                GetValue(values, "BottomFabric"),
                GetValue(values, "BottomCutSize"),
                ParseDouble(GetValue(values, "BottomTotalMtr")),
                ParseDouble(GetValue(values, "BottomTotalKg")),
                FirstNonEmpty(GetValue(values, "BottomRemarks"), GetValue(values, "BottomRemarks1")));

            AddDerivedLine(lines, ref sortOrder, "Bottom Spout",
                GetValue(values, "DSGSM"),
                GetValue(values, "DSLami"),
                GetValue(values, "DSColor"),
                GetValue(values, "DSFabric"),
                GetValue(values, "DSCutSize"),
                ParseDouble(GetValue(values, "DSTotalMtr")),
                ParseDouble(GetValue(values, "DSTotalKg")),
                FirstNonEmpty(GetValue(values, "DSRemarks"), request.Header.DSType));

            AddDerivedLine(lines, ref sortOrder, "Bottom Spout Tie",
                GetValue(values, "DSTieGSM"),
                GetValue(values, "DSTieLami"),
                GetValue(values, "DSTieColor"),
                GetValue(values, "DSTieFabric"),
                GetValue(values, "DSTieCutSize"),
                ParseDouble(GetValue(values, "DSTieTotalMtr")),
                ParseDouble(GetValue(values, "DSTieTotalKg")),
                FirstNonEmpty(GetValue(values, "DSTieRemarks"), GetValue(values, "DSTieRemarks1")));

            AddDerivedLine(lines, ref sortOrder, "Loop",
                GetValue(values, "LoopGSM"),
                "",
                GetValue(values, "LoopColor"),
                GetValue(values, "LoopFabric"),
                GetValue(values, "LoopCutSize"),
                ParseDouble(GetValue(values, "LoopTotalMtr")),
                ParseDouble(GetValue(values, "LoopTotalKg")),
                FirstNonEmpty(GetValue(values, "LoopRemarks"), GetValue(values, "LoopRemarks1")));

            AddDerivedLine(lines, ref sortOrder, "Liner",
                GetValue(values, "LinerGSM"),
                GetValue(values, "LinerLami"),
                GetValue(values, "LinerColor"),
                FirstNonEmpty(GetValue(values, "LinerFabric"), GetValue(values, "LinerL")),
                GetValue(values, "LinerCutSize"),
                ParseDouble(GetValue(values, "LinerTotalMtr")),
                ParseDouble(GetValue(values, "LinerTotalKg")),
                FirstNonEmpty(GetValue(values, "LinerRemarks"), GetValue(values, "LinerRemarks1")));

            AddDerivedLine(lines, ref sortOrder, "DocPouch",
                GetValue(values, "DocGSM"),
                GetValue(values, "DocLami"),
                GetValue(values, "DocColor"),
                GetValue(values, "DocFabric"),
                GetValue(values, "DocCutSize"),
                ParseDouble(GetValue(values, "DocTotalMtr")),
                ParseDouble(GetValue(values, "DocTotalKg")),
                GetValue(values, "DocRemarks"));

            AddDerivedLine(lines, ref sortOrder, "Label",
                GetValue(values, "LableGSM"),
                "",
                GetValue(values, "LabelColor"),
                GetValue(values, "LableFabric"),
                GetValue(values, "LableCutSize"),
                ParseDouble(GetValue(values, "LableTotalMtr")),
                ParseDouble(GetValue(values, "LableTotalKg")),
                FirstNonEmpty(GetValue(values, "LabelRemarks"), GetValue(values, "LabelRemarks1")));

            AddDerivedLine(lines, ref sortOrder, "Filler Cord",
                GetValue(values, "FillerCordGSM"),
                "",
                "",
                "",
                "",
                null,
                ParseDouble(GetValue(values, "FillerCordTotalKg")),
                "");

            AddDerivedLine(lines, ref sortOrder, "Thread",
                "",
                "",
                GetValue(request.Bom3Values, "threadColor"),
                "",
                "",
                0,
                ParseDouble(GetValue(values, "ThreadTotalKg")),
                GetValue(request.Bom3Values, "threadtype"));

            AddDerivedLineFromSources(lines, ref sortOrder, "IRIS Tie", values, editor,
                ["FSIRISTieGSM", "FSTieGSM"],
                [],
                ["TopSpoutTieIRISColor", "FSTieColor"],
                ["FSIRISTieFabric", "FSTieFabric"],
                ["FSIRISTieCutSize"],
                ["FSIRISTieTotalMtr"],
                ["FSIRISTieTotalKg"],
                ["TopSpoutTieIRISRemarks", "FSTieRemarks"]);

            AddDerivedLineFromSources(lines, ref sortOrder, "Top Flap", values, editor,
                ["TopFlapGSM"], ["TopFlapLamiType"], ["TopFlapColor"], ["TopFlapFabric"], ["TopFlapCutSize"], ["TopFlapTotalMtr"], ["TopFlapTotalKg"], ["TopFlapRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Top Rope", values, editor,
                ["TopRopeGSM"], ["TopRopeType"], ["TopRopeColor"], ["TopRopeFabric"], ["TopRopeCutSize", "TopRopeCutSizes"], ["TopRopeTotalMtr"], ["TopRopeTotalKg"], ["TopRopeRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "TopSpout Rope", values, editor,
                ["TopSpoutRopeGSM"], ["TopSpoutRopeType"], ["TopSpoutRopeColor"], ["TopSpoutRopeFabric"], ["TopSpoutRopeCutSize"], ["TopSpoutRopeTotalMtr"], ["TopSpoutRopeTotalKg"], ["TopSpoutRopeRemarks", "topspoutroperemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Top Petal Rope", values, editor,
                ["TopPetalRopeGSM"], ["TopPetalRopeType"], ["TopPetalRopeColor"], ["TopPetalRopeFabric"], ["TopPetalRopeCutSize"], ["TopPetalRopeTotalMtr"], ["TopPetalRopeTotalKg"], ["TopPetalRopeRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "IRIS Rope", values, editor,
                ["TopIrisRopeGSM"], ["TopIrisRopeType"], ["TopIrisRopeColor"], ["TopIrisRopeFabric"], ["TopIrisRopeCutSize"], ["TopIrisRopeTotalMtr"], ["TopIrisRopeTotalKg"], ["TopIrisRopeRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Top Petal Flap", values, editor,
                ["TopPetalFlapGSM"], ["TopPetalFlapLami"], ["TopPetalFlapColor"], ["TopPetalFlapFabric"], ["TopPetalFlapCutSize"], ["TopPetalFlapTotalMtr"], ["TopPetalFlapTotalKg"], ["TopPetalFlapRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Top Hook", values, editor,
                ["TopHookGSM"], ["TopHookType"], ["TopHookColor"], ["TopHookFabric"], ["TopHookCutSize"], ["TopHookTotalMtr"], ["TopHookTotalKg"], ["TopHookRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Top Tie", values, editor,
                ["TopTieGSM"], ["TopTieType"], ["TopTieColor"], ["TopTieFabric"], ["TopTieCutSize", "TopTieCutSizes"], ["TopTieTotalMtr"], ["TopTieTotalKg"], ["TopTieRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Top Band", values, editor,
                ["TopBandGSM"], ["TopBandType"], ["TopBandColor"], ["TopBandFabric"], ["TopBandCutSize"], ["TopBandTotalMtr"], ["TopBandTotalKg"], ["TopBandRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Belly Band 1", values, editor,
                ["TopBellyBand1GSM"], ["TopBellyBand1Type"], ["TopBellyBand1Color"], ["TopBellyBand1Fabric"], ["TopBellyBand1CutSize"], ["TopBellyBand1TotalMtr"], ["TopBellyBand1TotalKg"], ["TopBellyBand1Remarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Belly Band 2", values, editor,
                ["TopBellyBand2GSM"], ["TopBellyBand2Type"], ["TopBellyBand2Color"], ["TopBellyBand2Fabric"], ["TopBellyBand2CutSize"], ["TopBellyBand2TotalMtr"], ["TopBellyBand2TotalKg"], ["TopBellyBand2Remarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Band", values, editor,
                ["TopBottomBandGSM", "BottomBandGSM"], ["BottomBandType"], ["TopBottomBandColor", "BottomBandColor"], ["TopBottomBandFabric", "BottomBandFabric"], ["TopBottomBandCutSize", "BottomBandCutSize"], ["TopBottomBandTotalMtr", "BottomBandTotalMtr"], ["TopBottomBandTotalKg", "BottomBandTotalKg"], ["BottomBandRemarks"]);

            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Spout1", values, editor,
                ["BottomSpout1GSM", "BS1bottomgsm2"], ["BottomSpout1Lami", "BS1BottomLamiGSM1"], ["BottomSpout1Color", "BS1BottomColor1"], ["BottomSpout1Fabric", "DS1Fabric"], ["BottomSpout1CutSize", "DS1CutSize"], ["BottomSpout1TotalMtr", "DS1TotalMtr"], ["BottomSpout1TotalKg", "DS1TotalKg"], ["BottomSpout1Remarks", "BS1Bottomrem1"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Spout2", values, editor,
                ["BottomSpout2GSM", "BS2bottomgsm4"], ["BottomSpout2Lami", "BS2LamiGSM2"], ["BottomSpout2Color", "BS2Color2"], ["BottomSpout2Fabric", "DS2Fabric"], ["BottomSpout2CutSize", "DS2CutSize"], ["BottomSpout2TotalMtr", "DS2TotalMtr"], ["BottomSpout2TotalKg", "DS2TotalKg"], ["BottomSpout2Remarks", "BS2rem2"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "IRIS Bottom Tie", values, editor,
                ["DSIRISTieGSM", "DSTieGSM"], [], ["BottomSpoutTieIRISColor", "DSTieColor"], ["DSIRISTieFabric", "DSTieFabric"], ["DSIRISTieCutSize"], ["DSIRISTieTotalMtr"], ["DSIRISTieTotalKg"], ["BottomSpoutTieIRISRemarks", "DSTieRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Spout Tie1", values, editor,
                ["DSTie1GSM", "BS1TieGrm1"], [], ["DSTie1Color"], ["DSTie1Fabric", "BS1TieSize1"], ["DSTie1CutSize", "BS1TieCutSize1"], ["DSTie1TotalMtr"], ["DSTie1TotalKg"], ["DSTie1Remarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Spout Tie2", values, editor,
                ["DSTie2GSM", "BS2TieGrm2"], [], ["DSTie2Color"], ["DSTie2Fabric", "BS2TieSize2"], ["DSTie2CutSize", "BS2TieCutSize2"], ["DSTie2TotalMtr"], ["DSTie2TotalKg"], ["DSTie2Remarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Flap", values, editor,
                ["BottomFlapGSM"], ["BottomFlapLamiType"], ["BottomFlapColor"], ["BottomFlapFabric"], ["BottomFlapCutSize", "BottomFlapCutLenght"], ["BottomFlapTotalMtr"], ["BottomFlapTotalKg"], ["BottomFlapRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Rope", values, editor,
                ["BottomRopeGSM"], ["BottomRopeType"], ["BottomRopeColor"], ["BottomRopeFabric"], ["BottomRopeCutSize", "BottomRopeCutSizes"], ["BottomRopeTotalMtr"], ["BottomRopeTotalKg"], ["BottomRopeRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Duffle/Skrit", values, editor,
                ["BottomDuffleGSM"], ["BottomDuffleLamiType"], ["BottomDuffleColor"], ["BottomDuffleFabric"], ["BottomDuffleCutSize"], ["BottomDuffleTotalMtr"], ["BottomDuffleTotalKg"], ["BottomDuffleRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "BottomSpout Rope", values, editor,
                ["BottomSpoutRopeGSM"], ["BottomSpoutRopeType"], ["BottomSpoutRopeColor"], ["BottomSpoutRopeFabric"], ["BottomSpoutRopeCutSize"], ["BottomSpoutRopeTotalMtr"], ["BottomSpoutRopeTotalKg"], ["BottomSpoutRopeRemarks", "textBottomspoutroperemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Petal Rope", values, editor,
                ["BottomPetalRopeGSM"], ["BottomPetalRopeType"], ["BottomPetalRopeColor"], ["BottomPetalRopeFabric"], ["BottomPetalRopeCutSize"], ["BottomPetalRopeTotalMtr"], ["BottomPetalRopeTotalKg"], ["BottomPetalRopeRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Petal Flap", values, editor,
                ["BottomPetalFlapGSM"], ["BottomPetalFlapLami"], ["BottomPetalFlapColor"], ["BottomPetalFlapFabric"], ["BottomPetalFlapCutSize"], ["BottomPetalFlapTotalMtr"], ["BottomPetalFlapTotalKg"], ["BottomPetalFlapRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "IRIS Bottom Rope", values, editor,
                ["BottomIrisRopeGSM"], ["BottomIrisRopeType"], ["BottomIrisRopeColor"], ["BottomIrisRopeFabric"], ["BottomIrisRopeCutSize"], ["BottomIrisRopeTotalMtr"], ["BottomIrisRopeTotalKg"], ["BottomIrisRopeRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "BottomSpout Rope1", values, editor,
                ["BottomSpoutRope1GSM"], ["BottomSpoutRope1Type"], ["BottomSpoutRope1Color", "BS1SpoutRopeColor1"], ["BottomSpoutRope1Fabric"], ["BottomSpoutRope1CutSize", "BS1SpoutRopeSize1"], ["BottomSpoutRope1TotalMtr"], ["BottomSpoutRope1TotalKg"], ["BottomSpoutRope1Remarks", "BS1spoutroperemarks1"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Petal Rope1", values, editor,
                ["BottomPetalRope1GSM"], ["BottomPetalRope1Type"], ["BottomPetalRope1Color"], ["BottomPetalRope1Fabric"], ["BottomPetalRope1CutSize"], ["BottomPetalRope1TotalMtr"], ["BottomPetalRope1TotalKg"], ["BottomPetalRope1Remarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "BottomSpout Rope2", values, editor,
                ["BottomSpoutRope2GSM"], ["BottomSpoutRope2Type"], ["BottomSpoutRope2Color"], ["BottomSpoutRope2Fabric"], ["BottomSpoutRope2CutSize", "BS2SpoutRopeSize2"], ["BottomSpoutRope2TotalMtr"], ["BottomSpoutRope2TotalKg"], ["BottomSpoutRope2Remarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Petal Rope2", values, editor,
                ["BottomPetalRope2GSM"], ["BottomPetalRope2Type"], ["BottomPetalRope2Color"], ["BottomPetalRope2Fabric"], ["BottomPetalRope2CutSize"], ["BottomPetalRope2TotalMtr"], ["BottomPetalRope2TotalKg"], ["BottomPetalRope2Remarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Hook", values, editor,
                ["BottomHookGSM"], ["BottomHookType"], ["BottomHookColor"], ["BottomHookFabric"], ["BottomHookCutSize"], ["BottomHookTotalMtr"], ["BottomHookTotalKg"], ["BottomHookRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Tie", values, editor,
                ["BottomTieGSM"], ["BottomTieType"], ["BottomTieColor"], ["BottomTieFabric"], ["BottomTieCutSize", "BottomTieCutSize"], ["BottomTieTotalMtr"], ["BottomTieTotalKg"], ["BottomTieRemarks"]);

            AddDerivedLineFromSources(lines, ref sortOrder, "FabricPatch", values, editor,
                ["FabricPatchGSM"], ["FabricPatchLamiType"], ["FabricPatchColor"], ["FabricPatchFabric"], ["FabricPatchCutSize"], ["FabricPatchTotalMtr"], ["FabricPatchTotalKg"], ["FabricPatchRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Loop Cover", values, editor,
                ["LoopCoverGSM"], ["LoopCoverLamiType"], ["LoopCoverColor"], ["LoopCoverFabric"], ["LoopCoverCutSize"], ["LoopCoverTotalMtr"], ["LoopCoverTotalKg"], ["LoopCoverRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Stevedore Cover", values, editor,
                ["StevedoreCoverGSM"], ["StevedoreCoverLamiType"], ["StevedoreCoverColor"], ["StevedoreCoverFabric"], ["StevedoreCoverCutSize"], ["StevedoreCoverTotalMtr"], ["StevedoreCoverTotalKg"], ["StevedoreCoverRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Loop Protector", values, editor,
                ["LoopProtectorGSM"], ["LoopProtectorLamiType"], ["LoopProtectorColor"], ["LoopProtectorFabric"], ["LoopProtectorCutSize"], ["LoopProtectorTotalMtr"], ["LoopProtectorTotalKg"], ["LoopProtectorRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Stevedore", values, editor,
                ["StevedoreGSM"], ["StevedoreType"], ["StevedoreColor"], ["StevedoreFabric"], ["StevedoreCutSize"], ["StevedoreTotalMtr"], ["StevedoreTotalKg"], ["StevedoreRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Bottom Loop", values, editor,
                ["BottomLoopGSM"], ["BottomLoopType"], ["BottomLoopColor"], ["BottomLoopFabric"], ["BottomLoopCutSize"], ["BottomLoopTotalMtr"], ["BottomLoopTotalKg"], ["BottomLoopRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Liner Baffle", values, editor,
                ["LinerBaffleGSM"], ["LinerBaffleLamiType"], ["LinerBaffleColor"], ["LinerBaffleFabric"], ["LinerBaffleCutSize"], ["LinerBaffleTotalMtr"], ["LinerBaffleTotalKg"], ["LinerBaffleRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "DocPouch1", values, editor,
                ["Doc1GSM"], ["Doc1Lami"], ["Doc1Color"], ["Doc1Fabric"], ["Doc1CutSize"], ["Doc1TotalMtr"], ["Doc1TotalKg"], ["Doc1Remarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "DocPouch2", values, editor,
                ["Doc2GSM"], ["Doc2Lami"], ["Doc2Color"], ["Doc2Fabric"], ["Doc2CutSize"], ["Doc2TotalMtr"], ["Doc2TotalKg"], ["Doc2Remarks"]);
            var tunnelHeading = ResolveTunnelHeading(request);
            AddDerivedLineFromSources(lines, ref sortOrder, tunnelHeading, values, editor,
                ["TunnelGSM"], ["TunnelLami"], ["TunnelColor"], ["TunnelFabric"], ["TunnelCutSize"], ["TunnelTotalMtr"], ["TunnelTotalKg"], ["TunnelRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Buffle", values, editor,
                ["BuffleGSM"], ["BuffleLamiType"], ["BuffleColor"], ["BuffleFabric"], ["BuffleCutSize"], ["BuffleTotalMtr"], ["BuffleTotalKg"], ["BuffleRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Ancillay Loop", values, editor,
                ["AncillaryLoopGSM"], ["AncillaryLoopType"], ["AncillaryLoopColor"], ["AncillaryLoopFabric"], ["AncillaryLoopCutSize"], ["AncillaryLoopTotalMtr"], ["AncillaryLoopTotalKg"], ["AncillaryLoopRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Felt", values, editor,
                ["FeltGSM"], ["FeltType"], ["FeltColor"], ["FeltFabric"], ["FeltCutSize"], ["FeltTotalMtr"], ["FeltTotalKg"], ["FeltRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Felt-Under Loop", values, editor,
                ["FeltUnderLoopGSM"], ["FeltUnderLoopType"], ["FeltUnderLoopColor"], ["FeltUnderLoopFabric"], ["FeltUnderLoopCutSize"], ["FeltUnderLoopTotalMtr"], ["FeltUnderLoopTotalKg"], ["FeltUnderLoopRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "MFWeb", values, editor,
                ["MFWebGSM"], ["MFWebType"], ["MFWebColor"], ["MFWebFabric"], ["MFWebCutSize"], ["MFWebTotalMtr"], ["MFWebTotalKg"], ["MFWebRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Full Loop", values, editor,
                ["FullLoopGSM"], ["FullLoopType"], ["FullLoopColor"], ["FullLoopFabric"], ["FullLoopCutSize"], ["FullLoopTotalMtr"], ["FullLoopTotalKg"], ["FullLoopRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Inner Box", values, editor,
                ["InnerBoxGSM"], ["InnerBoxLamiType"], ["InnerBoxColor"], ["InnerBoxFabric"], ["InnerBoxCutSize"], ["InnerBoxTotalMtr"], ["InnerBoxTotalKg"], ["InnerBoxRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Inner Skin", values, editor,
                ["InnerSkinGSM"], ["InnerSkinLamiType"], ["InnerSkinColor"], ["InnerSkinFabric"], ["InnerSkinCutSize"], ["InnerSkinTotalMtr"], ["InnerSkinTotalKg"], ["InnerSkinRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Inner Top", values, editor,
                ["InnerTopGSM"], ["InnerTopLamiType"], ["InnerTopColor"], ["InnerTopFabric"], ["InnerTopCutSize"], ["InnerTopTotalMtr"], ["InnerTopTotalKg"], ["InnerTopRemarks"]);
            AddDerivedLineFromSources(lines, ref sortOrder, "Inner Bottom", values, editor,
                ["InnerBottomGSM"], ["InnerBottomLamiType"], ["InnerBottomColor"], ["InnerBottomFabric"], ["InnerBottomCutSize"], ["InnerBottomTotalMtr"], ["InnerBottomTotalKg"], ["InnerBottomRemarks"]);

            return lines;
        }

        private static void AddDerivedLine(
            ICollection<BomCreateLineInput> lines,
            ref int sortOrder,
            string heading,
            string gsm,
            string lami,
            string color,
            string fabricSize,
            string cutSize,
            double? totalMtr,
            double? totalKg,
            string remarks)
        {
            var hasMeaningfulData =
                !string.IsNullOrWhiteSpace(gsm) ||
                !string.IsNullOrWhiteSpace(lami) ||
                !string.IsNullOrWhiteSpace(color) ||
                !string.IsNullOrWhiteSpace(fabricSize) ||
                !string.IsNullOrWhiteSpace(cutSize) ||
                !string.IsNullOrWhiteSpace(remarks) ||
                totalMtr is not null ||
                totalKg is not null;

            if (!hasMeaningfulData)
                return;

            lines.Add(new BomCreateLineInput
            {
                SortOrder = sortOrder++,
                Heading = heading,
                Gsm = gsm,
                Lami = lami,
                Color = color,
                FabricSize = fabricSize,
                CutSize = cutSize,
                TotalMtr = totalMtr,
                TotalKg = totalKg,
                Remarks = remarks,
            });
        }

        private static void AddDerivedLineFromSources(
            ICollection<BomCreateLineInput> lines,
            ref int sortOrder,
            string heading,
            IReadOnlyDictionary<string, string?> bom1Values,
            IReadOnlyDictionary<string, string?> bom3Values,
            IEnumerable<string> gsmKeys,
            IEnumerable<string> lamiKeys,
            IEnumerable<string> colorKeys,
            IEnumerable<string> fabricKeys,
            IEnumerable<string> cutKeys,
            IEnumerable<string> totalMtrKeys,
            IEnumerable<string> totalKgKeys,
            IEnumerable<string> remarksKeys)
        {
            var gsm = GetAnyValue(bom1Values, bom3Values, gsmKeys);
            var lami = GetAnyValue(bom1Values, bom3Values, lamiKeys);
            var color = GetAnyValue(bom1Values, bom3Values, colorKeys);
            var fabric = GetAnyValue(bom1Values, bom3Values, fabricKeys);
            var cut = GetAnyValue(bom1Values, bom3Values, cutKeys);
            var totalMtr = ParseAnyDouble(bom1Values, bom3Values, totalMtrKeys);
            var totalKg = ParseAnyDouble(bom1Values, bom3Values, totalKgKeys);
            var remarks = GetAnyValue(bom1Values, bom3Values, remarksKeys);

            var hasMeaningfulData =
                totalKg is > 0 ||
                totalMtr is > 0 ||
                !string.IsNullOrWhiteSpace(fabric) ||
                !string.IsNullOrWhiteSpace(cut);

            if (!hasMeaningfulData)
                return;

            AddDerivedLine(lines, ref sortOrder, heading, gsm, lami, color, fabric, cut, totalMtr, totalKg, remarks);
        }

        private static double CalculateTotalKg(IEnumerable<BomCreateLineInput> lines) =>
            Math.Round(lines.Sum(line => line.TotalKg ?? 0d), 4, MidpointRounding.AwayFromZero);

        private static string GetValue(IReadOnlyDictionary<string, string?> values, string key) =>
            values.TryGetValue(key, out var value) ? value ?? "" : "";

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return "";
        }

        private static string GetAnyValue(
            IReadOnlyDictionary<string, string?> primary,
            IReadOnlyDictionary<string, string?> secondary,
            IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                var value = GetValue(primary, key);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;

                value = GetValue(secondary, key);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return "";
        }

        private static double? ParseAnyDouble(
            IReadOnlyDictionary<string, string?> primary,
            IReadOnlyDictionary<string, string?> secondary,
            IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                var value = ParseDouble(GetValue(primary, key));
                if (value is not null)
                    return value;

                value = ParseDouble(GetValue(secondary, key));
                if (value is not null)
                    return value;
            }

            return null;
        }

        private static string ResolveTunnelHeading(BomCreateRequest request)
        {
            var tunnelDesign = FirstNonEmpty(
                GetValue(request.Bom3Values, "TunnelDesign"),
                GetValue(request.Bom1Values, "TunnelDesign"));

            return tunnelDesign.Contains("Cross Corner", StringComparison.OrdinalIgnoreCase)
                   || tunnelDesign.Contains("Leno", StringComparison.OrdinalIgnoreCase)
                ? "Reinforce fabric"
                : "Tunnel";
        }

        private static string? EmptyToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static double? ParseDouble(string? value) =>
            double.TryParse(value, out var result) ? result : null;

        private static DateTime? ParseDate(string? value) =>
            DateTime.TryParse(value, out var result) ? result : null;

        private static (string Construction, string BodyStyle, string BodyGrade) ResolveBagTypeParts(BomCreateRequest request)
        {
            var parts = (request.Header.BagType ?? "")
                .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var construction = FirstNonEmpty(GetValue(request.Bom1Values, "Construction"), parts.ElementAtOrDefault(0));
            var bodyStyle = FirstNonEmpty(GetValue(request.Bom1Values, "BodyStyle"), parts.ElementAtOrDefault(1));
            var bodyGrade = FirstNonEmpty(GetValue(request.Bom1Values, "BodyGrade"), parts.ElementAtOrDefault(2));
            return (construction, bodyStyle, bodyGrade);
        }

        private static bool IsInnerPlacement(BomCreateRequest request) =>
            !string.Equals(request.Header.SizeType, "OUTER", StringComparison.OrdinalIgnoreCase);

        private static bool IsTruthy(string? value) =>
            value is not null && (
                value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase));

        private static double ResolveTieCutLength(string? remarks, string? cutSize)
        {
            var fromRemarks = ParseTrailingNumber(remarks);
            if (fromRemarks is > 0)
                return (fromRemarks.Value * 2) + 5;

            return ParseDouble(cutSize) ?? 0;
        }

        private static double? ParseTrailingNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(value, @"(\d+(\.\d+)?)");
            return match.Success ? ParseDouble(match.Groups[1].Value) : null;
        }

        private static double ResolveSingleDoubleMultiplier(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;
            if (value.Contains("double", StringComparison.OrdinalIgnoreCase) || value.Trim() == "2")
                return 2;
            if (value.Contains("single", StringComparison.OrdinalIgnoreCase) || value.Trim() == "1")
                return 1;
            return 0;
        }

        private static double ResolveThreadBuffleFactor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;
            if (value.Contains("All Seam", StringComparison.OrdinalIgnoreCase))
                return 16;
            if (value.Contains("Eight Seam", StringComparison.OrdinalIgnoreCase))
                return 12;
            if (value.Contains("Bag Corner Seam", StringComparison.OrdinalIgnoreCase))
                return 8;
            return 0;
        }

        private static double ResolveLinerDensity(string linerMaterial)
        {
            if (linerMaterial.Equals("LD", StringComparison.OrdinalIgnoreCase)
                || linerMaterial.Equals("LLD", StringComparison.OrdinalIgnoreCase))
                return 0.92d;
            if (linerMaterial.Equals("HD", StringComparison.OrdinalIgnoreCase))
                return 0.94d;
            if (linerMaterial.Equals("ALU", StringComparison.OrdinalIgnoreCase))
                return 1.1d;
            return 1d;
        }

        private static int ResolveSfBucket(string? sfRatio)
        {
            var value = NormalizeOptional(sfRatio) ?? "";
            if (value.StartsWith("8", StringComparison.OrdinalIgnoreCase))
                return 8;
            if (value.StartsWith("6", StringComparison.OrdinalIgnoreCase))
                return 6;
            return 5;
        }

        private static double ResolveCrossCornerExtra(int sfBucket, double swl)
        {
            if (sfBucket == 5)
            {
                if (swl <= 1250) return 70;
                if (swl <= 1500) return 80;
                if (swl <= 2000) return 90;
                return 0;
            }

            if (swl <= 1000) return 60;
            if (swl <= 1299) return 80;
            if (swl <= 1300) return 90;
            if (swl <= 2000) return 100;
            return 0;
        }

        private static void SetIfMissing(IDictionary<string, string?> values, string key, double value)
        {
            if (values.TryGetValue(key, out var existing) && !string.IsNullOrWhiteSpace(existing))
                return;
            values[key] = value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool NearlyEqual(double left, double right) =>
            Math.Abs(left - right) < 0.0001d;

        private void InvalidateBomCaches()
        {
            _cache.Remove(PartyMappingCacheKey);
            _cache.Remove(UsersCacheKey);
        }

        private sealed class BomPreviewSession
        {
            public BomDetailResult Detail { get; init; } = new();
            public BomPdfModel PdfModel { get; init; } = new();
        }
    }
}
