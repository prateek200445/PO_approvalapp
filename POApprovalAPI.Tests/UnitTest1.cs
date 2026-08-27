using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Tests;

public class BomCreationServiceTests
{
    [Fact]
    public async Task PreviewAsync_derives_upanel_body_and_side_lines()
    {
        var service = CreateService();
        var request = CreateBaseRequest();
        request.Bom1Values["BodyGSM"] = "180";
        request.Bom1Values["BodyLami"] = "20";
        request.Bom1Values["SideGSM"] = "180";
        request.Bom1Values["SideLami"] = "10";
        request.Bom1Values["bodyno"] = "1";

        var result = await service.PreviewAsync(request);

        var body = Assert.Single(result.Lines, line => line.Heading == "Body");
        Assert.Equal("111", body.FabricSize);
        Assert.Equal("345", body.CutSize);
        Assert.Equal(1725d, body.TotalMtr);
        Assert.Equal(0.7659d, body.TotalKg);

        var side = Assert.Single(result.Lines, line => line.Heading == "Side");
        Assert.Equal("101", side.FabricSize);
        Assert.Equal("131", side.CutSize);
        Assert.Equal(1310d, side.TotalMtr);
        Assert.Equal(0.5028d, side.TotalKg);
    }

    [Fact]
    public async Task PreviewAsync_derives_top_spout_tie_from_legacy_size_remark()
    {
        var service = CreateService();
        var request = CreateBaseRequest();
        request.Bom1Values["BodyGSM"] = "180";
        request.Bom1Values["BodyLami"] = "20";
        request.Bom1Values["FSTieGSM"] = "6";
        request.Bom1Values["FSTieFabric"] = "15";
        request.Bom1Values["FSTieRemarks"] = "Size: 20";
        request.Bom3Values["TopSpoutTieNo"] = "2";

        var result = await service.PreviewAsync(request);

        var tie = Assert.Single(result.Lines, line => line.Heading == "Top Spout Tie");
        Assert.Equal("15", tie.FabricSize);
        Assert.Equal("45", tie.CutSize);
        Assert.Equal(450d, tie.TotalMtr);
        Assert.Equal(0.0054d, tie.TotalKg);
    }

    [Fact]
    public async Task PreviewAsync_derives_docpouch_from_doc_header_and_dimensions()
    {
        var service = CreateService();
        var request = CreateBaseRequest();
        request.Bom1Values["BodyGSM"] = "180";
        request.Bom1Values["BodyLami"] = "20";
        request.Bom1Values["docl"] = "20";
        request.Bom1Values["docw"] = "15";
        request.Bom1Values["DocGSM"] = "100";
        request.Header.Doc = "Zip Lock/RHS Open(Top Seam)/NA/3";
        request.Header.DocNumber = "3";
        request.Header.DocUnit = "CMS";

        var result = await service.PreviewAsync(request);

        var doc = Assert.Single(result.Lines, line => line.Heading == "DocPouch");
        Assert.Equal("24", doc.FabricSize);
        Assert.Equal("15", doc.CutSize);
        Assert.Equal(75d, doc.TotalMtr);
        Assert.Equal(0.0231d, doc.TotalKg);
    }

    [Fact]
    public async Task PreviewAsync_derives_thread_weight_for_upanel_top_and_bottom_spouts()
    {
        var service = CreateService();
        var request = CreateBaseRequest();
        request.Bom1Values["BodyGSM"] = "180";
        request.Bom1Values["BodyLami"] = "20";
        request.Bom1Values["FSL"] = "35";
        request.Bom1Values["FSW"] = "40";
        request.Bom1Values["DSL"] = "35";
        request.Bom1Values["DSW"] = "50";
        request.Bom3Values["toptypes"] = "Top Spout";
        request.Bom3Values["bottomtypes"] = "Bottom Spout/Simple";

        var result = await service.PreviewAsync(request);

        var thread = Assert.Single(result.Lines, line => line.Heading == "Thread");
        Assert.Equal(0.0231d, thread.TotalKg);
    }

    [Fact]
    public async Task PreviewAsync_derives_top_iris_tie_row()
    {
        var service = CreateService();
        var request = CreateBaseRequest();
        request.Bom1Values["BodyGSM"] = "180";
        request.Bom1Values["BodyLami"] = "20";
        request.Bom1Values["FSTieGSM"] = "6";
        request.Bom1Values["FSL"] = "35";
        request.Bom3Values["TopSpoutTieIRISNo"] = "2";

        var result = await service.PreviewAsync(request);

        var irisTie = Assert.Single(result.Lines, line => line.Heading == "IRIS Tie");
        Assert.Equal(0.0174d, irisTie.TotalKg);
    }

    [Fact]
    public async Task PreviewAsync_uses_reinforce_fabric_heading_for_cross_corner_tunnel()
    {
        var service = CreateService();
        var request = CreateBaseRequest();
        request.Bom1Values["BodyGSM"] = "180";
        request.Bom1Values["BodyLami"] = "20";
        request.Bom1Values["TunnelGSM"] = "180";
        request.Bom1Values["TunnelFabric"] = "105";
        request.Bom1Values["TunnelCutSize"] = "22";
        request.Bom1Values["TunnelTotalMtr"] = "110";
        request.Bom1Values["TunnelTotalKg"] = "0.4158";
        request.Bom3Values["TunnelDesign"] = "Cross Corner";

        var result = await service.PreviewAsync(request);

        var tunnel = Assert.Single(result.Lines, line => line.Heading == "Reinforce fabric");
        Assert.Equal("22", tunnel.CutSize);
    }

    private static BomCreationService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ProductionConnection"] = "Server=(local);Database=master;Trusted_Connection=True;",
                ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=master;Trusted_Connection=True;",
                ["ConnectionStrings:LoginEntryConnection"] = "Server=(local);Database=master;Trusted_Connection=True;",
            })
            .Build();

        return new BomCreationService(
            new DatabaseService(configuration),
            new MemoryCache(new MemoryCacheOptions()));
    }

    private static BomCreateRequest CreateBaseRequest()
    {
        return new BomCreateRequest
        {
            Header = new BomCreateHeaderInput
            {
                FilePoNo = "QTN-001",
                Customer = "Test Customer",
                BagType = "UPanel/Non-Builder/Std",
                SizeL = 100,
                SizeW = 90,
                SizeH = 120,
                SizeType = "INNER",
                Qty = "500",
                UserName = "tester",
                Swl = "1000",
                SfRatio = "5:1",
            },
            Approvals = ["Marketing"],
            Bom1Values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            Bom3Values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
        };
    }
}
