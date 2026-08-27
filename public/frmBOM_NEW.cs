using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;
using System.Collections;
using CrystalDecisions.Windows.Forms;
using CrystalDecisions.Shared.Interop;
using CrystalDecisions.Shared;

namespace ERP
{
    public partial class frmBOM_NEW : Form
    {

        #region Variable
        string BOMNo = "";


        double BodyWt = 0;
        double BodyCutLenght = 0;
        double BodyFabricSize = 0;
        double BodyTotalMtr = 0;

        double SideWt = 0;
        double SideCutLenght = 0;
        double SideFabricSize = 0;
        double SideTotalMtr = 0;


        double TopWt = 0;
        double TopCutLenght = 0;
        double TopFabricSize = 0;
        double TopTotalMtr = 0;

        double DuffleWt = 0;
        double DuffleCutLenght = 0;
        double DuffleFabricSize = 0;
        double DuffleTotalMtr = 0;

        double BottomDuffleWt = 0;
        double BottomDuffleCutLenght = 0;
        double BottomDuffleFabricSize = 0;
        double BottomDuffleTotalMtr = 0;

        double BaseWt = 0;  // Base are same meaning Bottom
        double BaseCutLenght = 0;
        double BaseFabricSize = 0;
        double BaseTotalMtr = 0;


        double FSWt = 0;
        double FSCutLenght = 0;
        double FSFabricSize = 0;
        double FSTotalMtr = 0;

        double FSTieWt = 0;
        double FSTieCutLenght = 0;
        double FSTieFabricSize = 0;
        double FSTieTotalMtr = 0;

        //29.09.2021
        double FSIRISTieWt = 0;
        double FSIRISTieCutLenght = 0;
        double FSIRISTieFabricSize = 0;
        double FSIRISTieTotalMtr = 0;


        double TopTieWt = 0;
        double TopTieCutLenght = 0;
        double TopTieFabricSize = 0;
        double TopTieTotalMtr = 0;

        double DSWt = 0;
        double DSCutLenght = 0;
        double DSFabricSize = 0;
        double DSTotalMtr = 0;


        double DSWt1 = 0;
        double DSCutLenght1 = 0;
        double DSFabricSize1 = 0;
        double DSTotalMtr1 = 0;

        double DSWt2 = 0;
        double DSCutLenght2 = 0;
        double DSFabricSize2 = 0;
        double DSTotalMtr2 = 0;

        double DSTieWt = 0;
        double DSTieCutLenght = 0;
        double DSTieFabricSize = 0;
        double DSTieTotalMtr = 0;

        //29.09.2021
        double DSIRISTieWt = 0;
        double DSIRISTieCutLenght = 0;
        double DSIRISTieFabricSize = 0;
        double DSIRISTieTotalMtr = 0;


        double DSTieWt1 = 0;
        double DSTieCutLenght1 = 0;
        double DSTieFabricSize1 = 0;
        double DSTieTotalMtr1 = 0;

        double DSTieWt2 = 0;
        double DSTieCutLenght2 = 0;
        double DSTieFabricSize2 = 0;
        double DSTieTotalMtr2 = 0;


        double BottomTieWt = 0;
        double BottomTieCutLenght = 0;
        double BottomTieFabricSize = 0;
        double BottomTieTotalMtr = 0;

        double BottomLoopWt = 0;
        double BottomLoopLenght = 0;
        double BottomLoopSize = 0;
        double BottomLoopTotalMtr = 0;

        double LoopWt = 0;
        double LoopCutLenght = 0;
        double LoopFabricSize = 0;
        double LoopTotalMtr = 0;

        double FullLoopWt = 0;
        double FullLoopCutLenght = 0;
        double FullLoopFabricSize = 0;
        double FullLoopTotalMtr = 0;

        double LinerWt = 0;
        double LinerCutLenght = 0;
        double LinerFabricSize = 0;
        double LinerTotalMtr = 0;

        double LinerBuffleWt = 0;
        double LinerCutLenghtBuffle = 0;
        double LinerFabricSizeBuffle = 0;
        double LinerBuffleTotalMtr = 0;


        double TopFlapWt = 0;
        double TopFlapCutLenght = 0;
        double TopFlapFabricSize = 0;
        double TopFlapTotalMtr = 0;

        double BottomFlapWt = 0;
        double BottomFlapCutLenght = 0;
        double BottomFlapFabricSize = 0;
        double BottomFlapTotalMtr = 0;

        double TopHookWt = 0;
        double TopHookCutLenght = 0;
        double TopHookFabricSize = 0;
        double TopHookTotalMtr = 0;

        double BottomHookWt = 0;
        double BottomHookCutLenght = 0;
        double BottomHookFabricSize = 0;
        double BottomHookTotalMtr = 0;


        double TopRopeWt = 0;
        double TopRopeCutLenght = 0;
        double TopRopeFabricSize = 0;
        double TopRopeTotalMtr = 0;

        double BottomRopeWt = 0;
        double BottomRopeCutLenght = 0;
        double BottomRopeFabricSize = 0;
        double BottomRopeTotalMtr = 0;

        double TopSpoutRopeWt = 0;
        double TopSpoutRopeCutLenght = 0;
        double TopSpoutRopeFabricSize = 0;
        double TopSpoutRopeTotalMtr = 0;

        double BottomSpoutRopeWt = 0;
        double BottomSpoutRopeCutLenght = 0;
        double BottomSpoutRopeFabricSize = 0;
        double BottomSpoutRopeTotalMtr = 0;

        double BottomSpoutRopeWt1 = 0;
        double BottomSpoutRopeCutLenght1 = 0;
        double BottomSpoutRopeFabricSize1 = 0;
        double BottomSpoutRopeTotalMtr1 = 0;

        double BottomSpoutRopeWt2 = 0;
        double BottomSpoutRopeCutLenght2 = 0;
        double BottomSpoutRopeFabricSize2 = 0;
        double BottomSpoutRopeTotalMtr2 = 0;


        double BuffleWt = 0;
        double BuffleCutLenght = 0;
        double BuffleFabricSize = 0;
        double BuffleTotalMtr = 0;

        double LabelWt = 0;
        double LabelCutLenght = 0;
        double LabelFabricSize = 0;
        double LabelTotalMtr = 0;

        double ThreadWt = 0;


        double DocWt = 0;
        double DocCutLenght = 0;
        double DocFabricSize = 0;
        double DocTotalMtr = 0;

        double Doc1Wt = 0;
        double Doc1CutLenght = 0;
        double Doc1FabricSize = 0;
        double Doc1TotalMtr = 0;

        double Doc2Wt = 0;
        double Doc2CutLenght = 0;
        double Doc2FabricSize = 0;
        double Doc2TotalMtr = 0;

        double SlitHt = 0;
        double TotalHt = 0;


        double SpoutcoperWt = 0;
        double SafetyBandWt = 0;

        double TunnelTotalMtr = 0;
        double TunnelWt = 0;
        double TunnelCutLenght = 0;
        double TunnelFabricSize = 0;

        double FeltWt = 0;
        double FeltMtr = 0;

        double FeltUnderTheLoopWt = 0;
        double FeltUnderTheLoopMtr = 0;
        double FeltUnderTheLoopCutLenght = 0;
        double FeltUnderTheLoopFabricSize = 0;

        double MFWebWt = 0;
        double MFWebMtr = 0;

        double FillerCordWt = 0;
        double FillerCordMtr = 0;
        double FillerCordGSM = 0;

        double TopBandWt = 0;
        double TopBandCutLenght = 0;
        double TopBandFabricSize = 0;
        double TopBandTotalMtr = 0;

        double InnerBoxWt = 0;
        double InnerBoxCutLenght = 0;
        double InnerBoxFabricSize = 0;
        double InnerBoxTotalMtr = 0;

        double LoopCoverWt = 0;
        double LoopCOverTotalMtr = 0;

        double StevedoreWt = 0;
        double StevedoreCutLenght = 0;
        double StevedoreFabricSize = 0;
        double StevedoreTotalMtr = 0;

        double SteveCoverWt = 0;
        double StevecoverLenght = 0;
        double StevecoverFabricSize = 0;
        double StevecoverTotalMtr = 0;

        double LoopProtectorWt = 0;
        double LoopProtectorCutLenght = 0;
        double LoopProtectorFabricSize = 0;
        double LoopProtectorTotalMtr = 0;

        double InnerSkinWt = 0;
        double InnerSkinCutLenght = 0;
        double InnerSkinFabricSize = 0;
        double InnerSkinTotalMtr = 0;

        double InnerTopWt = 0;
        double InnerTopCutLenght = 0;
        double InnerTopFabricSize = 0;
        double InnerTopTotalMtr = 0;

        double InnerBottomWt = 0;
        double InnerBottomCutLenght = 0;
        double InnerBottomFabricSize = 0;
        double InnerBottomTotalMtr = 0;

        double AncerieWt = 0;
        double AncerieCutLenght = 0;
        double AncerieFabricSize = 0;
        double AncerieTotalMtr = 0;

        #region New fields 17.06.2021
        double FabricPatchCutLength = 0; //17.06.2021
        double FabricPatchSize = 10;//17.06.2021
        double FabricPatchWt = 0;//17.06.2021
        double FabricPatcTotalMtr = 0;//17.06.2021
        #endregion

        #region New fields 18.06.2021
        double TopBellyBand1Wt = 0;
        double TopBellyBand1CutLenght = 0;
        double TopBellyBand1FabricSize = 0;
        double TopBellyBand1TotalMtr = 0;

        double TopBellyBand2Wt = 0;
        double TopBellyBand2CutLenght = 0;
        double TopBellyBand2FabricSize = 0;
        double TopBellyBand2TotalMtr = 0;

        double TopBottomBandWt = 0;
        double TopBottomBandCutLenght = 0;
        double TopBottomBandFabricSize = 0;
        double TopBottomBandTotalMtr = 0;
        #endregion
        public static bool IsTemp = false;
        CurrencyManager currencyManager;
        double TotalKg = 0;

        bool IsupdateMode = false;

        public static string FilePONo = "";
        public static string CompanyName = "";

        bool btnupdateclick = false;

        #endregion

        double _BagHeight = 0;
        double _BagWidth = 0;
        double _BagLenght = 0;
        double _BagQty = 0;
        string _StrGSM = string.Empty;
        int _BodyIndex1;
        int _Type = 0;

        double _BagGSM = 0;
        double _BagLamiGSM = 0;
        double _BagSideGSM = 0;
        double _BagSideLamiGSM = 0;
        //Bottom Petal
        double PetalSize = 0;
        double PetalCutLength = 0;
        double PetalWT = 0;
        double PetalTotalMtr = 0;

        //Top Petal
        double TopPetalSize = 0;
        double TopPetalCutLength = 0;
        double TopPetalWT = 0;
        double TopPetalTotalMtr = 0;
        //double Conical
        //double Conical


        public frmBOM_NEW()
        {
            InitializeComponent();
            // Set the form size to match the screen size
            this.Width = Screen.PrimaryScreen.Bounds.Width;
            this.Height = Screen.PrimaryScreen.Bounds.Height;

            form_load();
        }


        public frmBOM_NEW(string FilePO)
        {
            InitializeComponent();
            // Set the form size to match the screen size
            this.Width = Screen.PrimaryScreen.Bounds.Width;
            this.Height = Screen.PrimaryScreen.Bounds.Height;

            form_load();
            textFilePONo.Text = FilePO;
            MessageBox.Show("To retrive BOM Data, please double click on Qtn No");
        }


        private void ClearVariables()
        {
            _BagHeight = 0;
            _BagWidth = 0;
            _BagLenght = 0;
            _BagQty = 0;
            _StrGSM = string.Empty;

            _Type = 0;

            _BagGSM = 0;
            _BagLamiGSM = 0;
            _BagSideGSM = 0;
            _BagSideLamiGSM = 0;
            //Bottom Petal
            PetalSize = 0;
            PetalCutLength = 0;
            PetalWT = 0;
            PetalTotalMtr = 0;

            //Top Petal
            TopPetalSize = 0;
            TopPetalCutLength = 0;
            TopPetalWT = 0;
            TopPetalTotalMtr = 0;

            BodyWt = 0;
            BodyCutLenght = 0;
            BodyFabricSize = 0;
            BodyTotalMtr = 0;

            SideWt = 0;
            SideCutLenght = 0;
            SideFabricSize = 0;
            SideTotalMtr = 0;


            TopWt = 0;
            TopCutLenght = 0;
            TopFabricSize = 0;
            TopTotalMtr = 0;

            DuffleWt = 0;
            DuffleCutLenght = 0;
            DuffleFabricSize = 0;
            DuffleTotalMtr = 0;

            BottomDuffleWt = 0;
            BottomDuffleCutLenght = 0;
            BottomDuffleFabricSize = 0;
            BottomDuffleTotalMtr = 0;

            BaseWt = 0;  // Base are same meaning Bottom
            BaseCutLenght = 0;
            BaseFabricSize = 0;
            BaseTotalMtr = 0;


            FSWt = 0;
            FSCutLenght = 0;
            FSFabricSize = 0;
            FSTotalMtr = 0;

            FSTieWt = 0;
            FSTieCutLenght = 0;
            FSTieFabricSize = 0;
            FSTieTotalMtr = 0;

            TopTieWt = 0;
            TopTieCutLenght = 0;
            TopTieFabricSize = 0;
            TopTieTotalMtr = 0;

            DSWt = 0;
            DSCutLenght = 0;
            DSFabricSize = 0;
            DSTotalMtr = 0;


            DSWt1 = 0;
            DSCutLenght1 = 0;
            DSFabricSize1 = 0;
            DSTotalMtr1 = 0;

            DSWt2 = 0;
            DSCutLenght2 = 0;
            DSFabricSize2 = 0;
            DSTotalMtr2 = 0;

            DSTieWt = 0;
            DSTieCutLenght = 0;
            DSTieFabricSize = 0;
            DSTieTotalMtr = 0;

            DSTieWt1 = 0;
            DSTieCutLenght1 = 0;
            DSTieFabricSize1 = 0;
            DSTieTotalMtr1 = 0;

            DSTieWt2 = 0;
            DSTieCutLenght2 = 0;
            DSTieFabricSize2 = 0;
            DSTieTotalMtr2 = 0;


            BottomTieWt = 0;
            BottomTieCutLenght = 0;
            BottomTieFabricSize = 0;
            BottomTieTotalMtr = 0;

            BottomLoopWt = 0;
            BottomLoopLenght = 0;
            BottomLoopSize = 0;
            BottomLoopTotalMtr = 0;

            LoopWt = 0;
            LoopCutLenght = 0;
            LoopFabricSize = 0;
            LoopTotalMtr = 0;

            FullLoopWt = 0;
            FullLoopCutLenght = 0;
            FullLoopFabricSize = 0;
            FullLoopTotalMtr = 0;

            LinerWt = 0;
            LinerCutLenght = 0;
            LinerFabricSize = 0;
            LinerTotalMtr = 0;

            LinerBuffleWt = 0;
            LinerCutLenghtBuffle = 0;
            LinerFabricSizeBuffle = 0;
            LinerBuffleTotalMtr = 0;


            TopFlapWt = 0;
            TopFlapCutLenght = 0;
            TopFlapFabricSize = 0;
            TopFlapTotalMtr = 0;

            BottomFlapWt = 0;
            BottomFlapCutLenght = 0;
            BottomFlapFabricSize = 0;
            BottomFlapTotalMtr = 0;

            TopHookWt = 0;
            TopHookCutLenght = 0;
            TopHookFabricSize = 0;
            TopHookTotalMtr = 0;

            BottomHookWt = 0;
            BottomHookCutLenght = 0;
            BottomHookFabricSize = 0;
            BottomHookTotalMtr = 0;


            TopRopeWt = 0;
            TopRopeCutLenght = 0;
            TopRopeFabricSize = 0;
            TopRopeTotalMtr = 0;

            BottomRopeWt = 0;
            BottomRopeCutLenght = 0;
            BottomRopeFabricSize = 0;
            BottomRopeTotalMtr = 0;

            TopSpoutRopeWt = 0;
            TopSpoutRopeCutLenght = 0;
            TopSpoutRopeFabricSize = 0;
            TopSpoutRopeTotalMtr = 0;

            BottomSpoutRopeWt = 0;
            BottomSpoutRopeCutLenght = 0;
            BottomSpoutRopeFabricSize = 0;
            BottomSpoutRopeTotalMtr = 0;

            BottomSpoutRopeWt1 = 0;
            BottomSpoutRopeCutLenght1 = 0;
            BottomSpoutRopeFabricSize1 = 0;
            BottomSpoutRopeTotalMtr1 = 0;

            BottomSpoutRopeWt2 = 0;
            BottomSpoutRopeCutLenght2 = 0;
            BottomSpoutRopeFabricSize2 = 0;
            BottomSpoutRopeTotalMtr2 = 0;


            BuffleWt = 0;
            BuffleCutLenght = 0;
            BuffleFabricSize = 0;
            BuffleTotalMtr = 0;

            LabelWt = 0;
            LabelCutLenght = 0;
            LabelFabricSize = 0;
            LabelTotalMtr = 0;

            ThreadWt = 0;


            DocWt = 0;
            DocCutLenght = 0;
            DocFabricSize = 0;
            DocTotalMtr = 0;

            Doc1Wt = 0;
            Doc1CutLenght = 0;
            Doc1FabricSize = 0;
            Doc1TotalMtr = 0;

            Doc2Wt = 0;
            Doc2CutLenght = 0;
            Doc2FabricSize = 0;
            Doc2TotalMtr = 0;

            SlitHt = 0;
            TotalHt = 0;


            SpoutcoperWt = 0;
            SafetyBandWt = 0;

            TunnelTotalMtr = 0;
            TunnelWt = 0;
            TunnelCutLenght = 0;
            TunnelFabricSize = 0;

            FeltWt = 0;
            FeltMtr = 0;

            FeltUnderTheLoopWt = 0;
            FeltUnderTheLoopMtr = 0;
            FeltUnderTheLoopCutLenght = 0;
            FeltUnderTheLoopFabricSize = 0;



            FillerCordWt = 0;
            FillerCordMtr = 0;
            FillerCordGSM = 0;

            TopBandWt = 0;
            TopBandCutLenght = 0;
            TopBandFabricSize = 0;
            TopBandTotalMtr = 0;

            InnerBoxWt = 0;
            InnerBoxCutLenght = 0;
            InnerBoxFabricSize = 0;
            InnerBoxTotalMtr = 0;

            LoopCoverWt = 0;
            LoopCOverTotalMtr = 0;

            SteveCoverWt = 0;
            StevecoverLenght = 0;
            StevecoverFabricSize = 0;
            StevecoverTotalMtr = 0;

            StevedoreWt = 0;
            StevedoreCutLenght = 0;
            StevedoreFabricSize = 0;
            StevedoreTotalMtr = 0;

            LoopProtectorWt = 0;
            LoopProtectorCutLenght = 0;
            LoopProtectorFabricSize = 0;
            LoopProtectorTotalMtr = 0;

            InnerSkinWt = 0;
            InnerSkinCutLenght = 0;
            InnerSkinFabricSize = 0;
            InnerSkinTotalMtr = 0;

            InnerTopWt = 0;
            InnerTopCutLenght = 0;
            InnerTopFabricSize = 0;
            InnerTopTotalMtr = 0;

            InnerBottomWt = 0;
            InnerBottomCutLenght = 0;
            InnerBottomFabricSize = 0;
            InnerBottomTotalMtr = 0;

            AncerieWt = 0;
            AncerieCutLenght = 0;
            AncerieFabricSize = 0;
            AncerieTotalMtr = 0;

            #region New fields 17.06.2021
            FabricPatchCutLength = 0; //17.06.2021
            FabricPatchSize = 10;//17.06.2021
            FabricPatchWt = 0;//17.06.2021
            FabricPatcTotalMtr = 0;//17.06.2021
            #endregion

            #region New fields 18.06.2021
            TopBellyBand1Wt = 0;
            TopBellyBand1CutLenght = 0;
            TopBellyBand1FabricSize = 0;
            TopBellyBand1TotalMtr = 0;

            TopBellyBand2Wt = 0;
            TopBellyBand2CutLenght = 0;
            TopBellyBand2FabricSize = 0;
            TopBellyBand2TotalMtr = 0;

            TopBottomBandWt = 0;
            TopBottomBandCutLenght = 0;
            TopBottomBandFabricSize = 0;
            TopBottomBandTotalMtr = 0;
            #endregion
        }
        private void form_load()
        {
            int i;

            /// added on 19th july 2022 
            //dataGridView1.Rows.Clear();
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[0].Cells[0].Value = "Top Spout Velcro";
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[1].Cells[0].Value = "Top Flap Velcro";
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[2].Cells[0].Value = "Bottom Spout Velcro";
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[3].Cells[0].Value = "Bottom Flap Velcro";

            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[4].Cells[0].Value = "FS B Lock";
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[5].Cells[0].Value = "DS B Lock";
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[6].Cells[0].Value = "Petal/Iris B Lock";

            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[7].Cells[0].Value = "DS Elastic band";

            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[8].Cells[0].Value = "Petal/Iris Elastic band";
            /// end on 19th July 2022

            dgapprovallist.Rows.Clear();
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[0].Cells[1].Value = "Label Approval";
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[1].Cells[1].Value = "Art Work Approval";
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[2].Cells[1].Value = "Print Proof Approval";
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[3].Cells[1].Value = "Bag Photo Approval";
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[4].Cells[1].Value = "Bag Sample Approval";
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[5].Cells[1].Value = "UN Certificate";
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[6].Cells[1].Value = "Bag Certificate";
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[7].Cells[1].Value = "Fabric Color Approval";
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[8].Cells[1].Value = "Loop Color Approval";
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[9].Cells[1].Value = "No Input Plan before Customer Approval";
            dgapprovallist.Rows.Add();
            dgapprovallist.Rows[10].Cells[1].Value = "No Approval Needed";


            


            comboBodyLamiGSM.Visible = false;
            comboBagType.Text = "Type A";
            comboSpoutLamiGSM.Visible = false;
            comboBottomLamiGSM.Visible = false;
            comboTopLamiGSM.Visible = false;
            comboBoxBottomSubTypeLamiGSM.Visible = false;
            comboLoopCoverLamiGSM.Visible = false;
            comboTunnelLamiGSM.Visible = false;
            comboTopflapLamiGsm.Visible = false;
            comboBottomflapLamiGSM.Visible = false;
            comboSideLamiGSM.Visible = false;
            comboInnerBoxLamiGSM.Visible = false;
            comboInnerBottomLamiGSM.Visible = false;
            comboInnerTopLamiGSM.Visible = false;
            comboInnerSkinLamiGSM.Visible = false;
            comboLoopProctectorLamiGSM.Visible = false;

            groupBoxdocpouch1.Visible = false;
            groupBoxdocpouch2.Visible = false;
            groupTopTie.Visible = false;
            groupBottomTie.Visible = false;
            groupBoxstevdore.Visible = false;
            groupSpout.Visible = false;
            groupBox6.Visible = false;
            groupBoxduffleskirt.Visible = false;
            groupTunnel.Visible = false;
            groupTop.Visible = false;
            groupBoxtopband.Visible = false;
            groupBoxtopflap.Visible = false;
            groupboxbottom.Visible = false;
            groupBoxbottomflap.Visible = false;
            groupBoxliner.Visible = false;
            groupBoxlabel.Visible = false;
            groupBoxblock.Visible = false;
            groupBoxdocpouch.Visible = false;
            groupBuffle.Visible = false;
            groupSingleLoop.Visible = false;
            groupTopSpoutRope.Visible = false;
            groupTopSpoutTie.Visible = false;
            groupBottomSpoutRope.Visible = false;
            groupBottomSpoutRope1.Visible = false;
            groupBottomSpoutRope2.Visible = false;
            groupBottomSpoutTie.Visible = false;
            groupBottomLoop.Visible = false;
            groupTopHook.Visible = false;
            groupBottomHook.Visible = false;
            groupTopRope.Visible = false;
            groupBottomRope.Visible = false;
            groupFillerCord.Visible = false;
            groupDropLoop.Visible = false;
            groupLoopCover.Visible = false;
            groupSide.Visible = false;
            groupInnerBox.Visible = false;
            groupInnerSkin.Visible = false;
            groupInnerTop.Visible = false;
            groupInnerBottom.Visible = false;
            groupLoops.Visible = false;
            groupLoopProc.Visible = false;
            groupBuffleSeam.Visible = false;
            groupThread.Visible = false;
            //groupHiracle.Visible = false;
            groupStevedorecover.Visible = false;

            EnqdateTime.CustomFormat = "MM/dd/yyyy";
            EnqdateTime.Format = DateTimePickerFormat.Custom;

            for (i = 0; i <= 500; i++)
            {
                comboLoopL.Items.Add(i);
                comboLoopW.Items.Add(i);
                comboStSize.Items.Add(i);
                comboSpoutDia.Items.Add(i);
                comboSpoutHeight.Items.Add(i);
                comboBoxbottomdia.Items.Add(i);

                comboBottomFlapHookCutsize.Items.Add(i);
                comboBottomFlapHookGrm.Items.Add(i);
                comboBottomFlapHookSize.Items.Add(i);

                comboTopflapHookGrm.Items.Add(i);
                comboTopFlapHookCutSize.Items.Add(i);
                comboTopflapHookSize.Items.Add(i);

                comboBoxbottomgsm.Items.Add(i);
                comboBoxbottomgsm1.Items.Add(i);
                comboBoxbottomheight.Items.Add(i);
                comboConicalHeight.Items.Add(i);
                comboBottomhoseslider.Items.Add(i);
                combobottomvelcro.Items.Add(i);
                comboBoxduffleskirtheight.Items.Add(i);
                comboBoxlineratpoint.Items.Add(i);
                //comboBoxlinerflange.Items.Add(i);
                comboBoxlinerheight.Items.Add(i);
                comboBoxlinermicron.Items.Add(i);
                comboBoxlinerwidth.Items.Add(i);
                comboBoxpacking.Items.Add(i);
                comboBoxtopflapdring.Items.Add(i);
                comboBoxtopflapgsm.Items.Add(i);
                comboBuffleGSM.Items.Add(i);
                combotopbandgrm.Items.Add(i);
                comboTunnelLen.Items.Add(i);
                comboTunnelWid.Items.Add(i);
                comboBodyLamiGSM.Items.Add(i);
            }


            if (checkBoxLoop.Checked == false)
                comboLoopConst.SelectedIndex = 0;
            comboLoopType.SelectedIndex = 0;
            comboLoopProtector.SelectedIndex = 0;

            comboTopType.SelectedIndex = 0;
            comboType.SelectedIndex = 0;
            comboBody1.SelectedIndex = 0;
            comboBodyGSM.SelectedIndex = 0;
            comboBodyUnit.SelectedIndex = 0;
            comboBody2.SelectedIndex = 0;
            comboBody3.SelectedIndex = 0;
            comboBoxbottomdia.SelectedIndex = 0;
            comboBoxbottomgsm.SelectedIndex = 0;
            comboBoxbottomgsm1.SelectedIndex = 0;
            comboBoxbottomheight.SelectedIndex = 0;
            comboBottomhoseslider.SelectedIndex = 0;
            combobottomvelcro.SelectedIndex = 0;
            comboBoxduffleskirtheight.SelectedIndex = 0;

            comboTopHoseSlider.SelectedIndex = 0;
            comboBoxlineratpoint.SelectedIndex = 0;
            //   comboBoxlinerflange.SelectedIndex = 0;
            comboBoxlinerheight.SelectedIndex = 0;
            comboBoxlinermicron.SelectedIndex = 0;
            comboBoxlinertype.SelectedIndex = 0;
            comboBoxlinertype1.SelectedIndex = 0;
            comboBoxlinerwidth.SelectedIndex = 0;
            comboBoxpacking.SelectedIndex = 0;
            comboBoxtopflapdring.SelectedIndex = 0;
            comboBoxtopflapgsm.SelectedIndex = 0;
            comboBoxtransport.SelectedIndex = 0;
            comboTopVelcro.SelectedIndex = 0;
            comboBuffleGSM.SelectedIndex = 0;
            comboLoopConst.SelectedIndex = 0;
            comboLoopGrm.SelectedIndex = 0;
            comboLoopL.SelectedIndex = 0;
            comboLoopProtector.SelectedIndex = 0;
            comboLoopType.SelectedIndex = 0;
            comboLoopW.SelectedIndex = 0;
            comboSF.SelectedIndex = 0;
            comboSpoutGSM.SelectedIndex = 0;
            comboSpoutType.SelectedIndex = 0;
            comboStGrm.SelectedIndex = 0;
            comboStSize.SelectedIndex = 0;
            comboSWLUnit.SelectedIndex = 1;
            //combotopbandgrm.SelectedIndex = 0;
            comboTopType.SelectedIndex = 0;
            comboTunnelGSM.SelectedIndex = 0;
            comboTunnelLen.SelectedIndex = 0;
            comboTunnelWid.SelectedIndex = 0;
            comboBodyLamiGSM.SelectedIndex = 0;
            comboSpoutLamiGSM.SelectedIndex = 0;
            comboBoxTopGSM.SelectedIndex = 0;
            comboTopLamiGSM.SelectedIndex = 0;
            comboSpoutDia.SelectedIndex = 0;
            comboSpoutHeight.SelectedIndex = 0;
            comboBoxbottomtype.SelectedIndex = 0;
            comboBoxBottomSubTypeLamiGSM.SelectedIndex = 0;
            comboBottomLamiGSM.SelectedIndex = 0;
            checkBoxlabel.Checked = true;
            comboBoxbottomsubtype.SelectedIndex = 5;
            btnUpdate.Enabled = false;


            string[] arrColor = { "Milky White", "Blue", "Orange", "Red", "Green", "Beige", "Black" };
            comboBodyColor.Items.AddRange(arrColor);
            comboBottomColor.Items.AddRange(arrColor);
            comboTopColor.Items.AddRange(arrColor);
            comboTopFlapColor.Items.AddRange(arrColor);
            comboAncerieColor.Items.AddRange(arrColor);
            comboBottomFlapColor.Items.AddRange(arrColor);
            comboBottomHookColor.Items.AddRange(arrColor);
            comboThreadColor.Items.AddRange(arrColor);
            comboBottomRopeColor.Items.AddRange(arrColor);
            comboBottomSpoutColor.Items.AddRange(arrColor);
            comboBottomSpoutColor2.Items.AddRange(arrColor);
            comboBottomSpoutColor1.Items.AddRange(arrColor);

            comboBottomSpoutRopeColor.Items.AddRange(arrColor);
            comboBottomSpoutTieColor.Items.AddRange(arrColor);
            comboBottomTieColor.Items.AddRange(arrColor);
            comboDocColor.Items.AddRange(arrColor);
            comboInnerBoxColor.Items.AddRange(arrColor);
            comboInnerSkinColor.Items.AddRange(arrColor);
            comboInnerTopColor.Items.AddRange(arrColor);
            comboLabelColor.Items.AddRange(arrColor);
            comboLinerColor.Items.AddRange(arrColor);
            comboLoopColor.Items.AddRange(arrColor);
            comboLoopCoverColor.Items.AddRange(arrColor);
            comboLoopProctectorColor.Items.AddRange(arrColor);
            comboSideColor.Items.AddRange(arrColor);
            comboSpoutColor.Items.AddRange(arrColor);
            comboSteveDoreColor.Items.AddRange(arrColor);
            comboTopBandColor.Items.AddRange(arrColor);
            comboTopFlapColor.Items.AddRange(arrColor);
            comboTopHookColor.Items.AddRange(arrColor);
            comboTopRopeColor.Items.AddRange(arrColor);
            comboTopSpoutRopeColor.Items.AddRange(arrColor);
            comboTopSpoutTieColor.Items.AddRange(arrColor);
            comboTopTieColor.Items.AddRange(arrColor);
            comboTunnelColor.Items.AddRange(arrColor);
            comboDoc1Color.Items.AddRange(arrColor);
            comboDoc2Color.Items.AddRange(arrColor);

            textPerson.Text = FrmMainForm.UserName;
            if (Database.OpenConnection(Utility.DespatchConnectionString))
            {
                Database.myreader = Database.GetExecuteReaderCommand("select distinct CompanyName  from CompanyMaster order by CompanyName");
                while (Database.myreader.Read())
                    comboPartyName.Items.Add(Database.myreader[0].ToString());
                Database.myreader.Close();
                Database.Closeconnection();
            }

            if (comboPartyName.Items.Count > 0)
                comboPartyName.SelectedIndex = 0;

            comboCurrency.SelectedIndex = 1;
        }

        private void frmBOM_Load(object sender, EventArgs e)
        {
            //this.KeyPreview = true;

        }

        private void AncerieWtFormula()
        {
            AncerieFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
            AncerieCutLenght = (Utility.SafeConvertToDouble(comboAncerieSize.Text) * 2 + Utility.SafeConvertToDouble(textBoxAtt.Text));
            AncerieWt = AncerieCutLenght * Utility.SafeConvertToDouble(comboAncerieGrm.Text)
           * Utility.SafeConvertToDouble(textNosAncerieLoop.Text);
            AncerieWt = AncerieWt / 100000;
            AncerieTotalMtr = AncerieCutLenght * _BagQty * 1
                    * Utility.SafeConvertToDouble(textNosAncerieLoop.Text);
            AncerieTotalMtr = AncerieTotalMtr / 100;

        }

        private void StevedoreWtFormula()
        {
            if (textstevelenght.Text != "")
            {
                StevedoreCutLenght = Utility.SafeConvertToDouble(textstevelenght.Text);
                StevedoreFabricSize = Utility.SafeConvertToDouble(comboStSize.Text);
                StevedoreWt = StevedoreCutLenght * Utility.SafeConvertToDouble(comboStGrm.Text) * Utility.SafeConvertToDouble(textStNo.Text);
            }
            else
            {
                if (comboStPortion.SelectedIndex == 0) // Lenght Portion
                {
                    if (_Type == 0) // Internal
                    {
                        StevedoreCutLenght = _BagLenght * 2 + 20;
                        StevedoreFabricSize = Utility.SafeConvertToDouble(comboStSize.Text);
                        //StevedoreWt = StevedoreCutLenght * Utility.SafeConvertToDouble(comboStGrm.Text) * Utility.SafeConvertToDouble(textStNo.Text);
                    }
                    else
                    {
                        StevedoreFabricSize = Utility.SafeConvertToDouble(comboStSize.Text);
                        StevedoreCutLenght = _BagLenght * 2 + 15;

                    } StevedoreWt = StevedoreCutLenght * Utility.SafeConvertToDouble(comboStGrm.Text) * Utility.SafeConvertToDouble(textStNo.Text);

                }
                else if (comboStPortion.SelectedIndex == 1) // Width Portion
                {
                    if (_Type == 0) // Internal
                    {
                        StevedoreCutLenght = _BagWidth * 2 + 20;
                        StevedoreFabricSize = Utility.SafeConvertToDouble(comboStSize.Text);
                        //StevedoreWt = StevedoreCutLenght * Utility.SafeConvertToDouble(comboStGrm.Text) * Utility.SafeConvertToDouble(textStNo.Text);
                    }
                    else
                    {
                        StevedoreFabricSize = Utility.SafeConvertToDouble(comboStSize.Text);
                        StevedoreCutLenght = _BagWidth * 2 + 15;

                    }
                    StevedoreWt = StevedoreCutLenght * Utility.SafeConvertToDouble(comboStGrm.Text) * Utility.SafeConvertToDouble(textStNo.Text);
                }
                else if (comboStPortion.SelectedIndex == 2) // Diagonal Portion
                {
                    if (_Type == 0) // Internal
                    {
                        StevedoreCutLenght = Math.Sqrt((_BagWidth * _BagWidth) + (_BagLenght * _BagLenght)) * 2 + 20;
                        StevedoreFabricSize = Utility.SafeConvertToDouble(comboStSize.Text);
                        // StevedoreWt = StevedoreCutLenght * Utility.SafeConvertToDouble(comboStGrm.Text) * Utility.SafeConvertToDouble(textStNo.Text);
                    }
                    else
                    {
                        StevedoreFabricSize = Math.Sqrt((_BagWidth * _BagWidth) + (_BagLenght * _BagLenght)) * 2 + 15;
                        StevedoreCutLenght = _BagWidth * 2 + 15;

                    } StevedoreWt = StevedoreCutLenght * Utility.SafeConvertToDouble(comboStGrm.Text) * Utility.SafeConvertToDouble(textStNo.Text);
                }

            }
            StevedoreWt = StevedoreWt / 100000;
            StevedoreWt = Math.Round(StevedoreWt, 4);
            StevedoreTotalMtr = ((StevedoreCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textStNo.Text)) + .1 * ((StevedoreCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textStNo.Text));
            StevedoreTotalMtr = Math.Round(StevedoreTotalMtr, 2);
        }

        # region Loop
        private void LoopProtectorWtFormula()
        {
            if (comboLoopProtector.SelectedIndex == 1) //Webbing/Reinforce
            {

                LoopProtectorFabricSize = Utility.SafeConvertToDouble(comboLoopProtectorSize.Text);
                LoopProtectorCutLenght = Utility.SafeConvertToDouble(comboLoopL.Text) * 2 - 5;
                LoopProtectorWt = LoopProtectorCutLenght * Utility.SafeConvertToDouble(textLoopNo.Text) * Utility.SafeConvertToDouble(comboLoopProtectorGSM.Text);

            }
            else if (comboLoopProtector.SelectedIndex == 2) //Fabric
            {
                LoopProtectorFabricSize = 16;
                LoopProtectorCutLenght = Utility.SafeConvertToDouble(comboLoopL.Text) * 2;
                LoopProtectorWt = LoopProtectorCutLenght * Utility.SafeConvertToDouble(textLoopNo.Text) * LoopProtectorFabricSize * (Utility.SafeConvertToDouble(comboLoopProtectorGSM.Text) + Utility.SafeConvertToDouble(comboLoopProctectorLamiGSM.Text));
                LoopProtectorWt = LoopProtectorWt / 100;
            }
            LoopProtectorWt = LoopProtectorWt / 100000; //change by anjul on dated 21st July 2017
            LoopProtectorWt = Math.Round(LoopProtectorWt, 4);
            LoopProtectorTotalMtr = ((LoopProtectorCutLenght / 100) * Utility.SafeConvertToDouble(textLoopNo.Text) * _BagQty);
               //+ .1 * ((LoopProtectorCutLenght / 100) * Utility.SafeConvertToDouble(textLoopNo.Text) * _BagQty);
            LoopProtectorTotalMtr = Math.Round(LoopProtectorTotalMtr, 2);
        }

        private void LoopCoverWtFormula()
        {
            LoopCOverTotalMtr = ((Utility.SafeConvertToDouble(comboLoopCoverCutSize.Text) * _BagQty) / 100);
                          //  + .1 * ((Utility.SafeConvertToDouble(comboLoopCoverCutSize.Text) * _BagQty) / 100);

            LoopCoverWt = ((Utility.SafeConvertToDouble(comboLoopCoverCutSize.Text) * Utility.SafeConvertToDouble(comboLoopCoverSize.Text) * Utility.SafeConvertToDouble(textLoopCoverNo.Text)
                    * (Utility.SafeConvertToDouble(comboLoopCoverGSM.Text) + Utility.SafeConvertToDouble(comboLoopCoverLamiGSM.Text))) / 10000000);
        }

        private void SteveCoverWtFormula()
        {
            StevecoverFabricSize = (Utility.SafeConvertToDouble(comboStSize.Text) * 2) + 5;
            StevecoverLenght = Utility.SafeConvertToDouble(textSteveCoverLengt.Text);

            StevecoverTotalMtr = ((StevecoverLenght * _BagQty * Utility.SafeConvertToDouble(textSteveCoverNo.Text)) / 100);
               //  + .1 * ((StevecoverLenght * _BagQty * Utility.SafeConvertToDouble(textSteveCoverNo.Text)) / 100);

            SteveCoverWt = ((StevecoverFabricSize * StevecoverLenght * Utility.SafeConvertToDouble(textSteveCoverNo.Text)
                    * (Utility.SafeConvertToDouble(comboSteveCoverL.Text) + Utility.SafeConvertToDouble(comboSteveCoverGSM.Text))) / 10000000);
        }

        private void FabricPatchWtFormula()
        {
            #region Add New Fabric Patch 17.06.2021
            FabricPatchCutLength = ((LoopCutLenght - (Utility.SafeConvertToDouble(comboLoopL.Text)) * 2) / 2) + 5;
            FabricPatchWt = (Utility.SafeConvertToDouble(cmbfabricpatchGSM.Text) + Utility.SafeConvertToDouble(cmbfabricPatchLamGSM.Text)) * FabricPatchSize * FabricPatchCutLength * 8;
            #endregion
        }
        #endregion

        private void InnerSkinWtFormula()
        {
            double InnerSkinExtraCutLenght = Utility.SafeConvertToDouble(textInnerSkinExtraCutLenght.Text);
            double InnerSkinGSM = Utility.SafeConvertToDouble(comboInnerSkinGSM.Text);
            double InnerSkinLamiGSM = Utility.SafeConvertToDouble(comboInnerSkinLamiGSM.Text);

            if (_BodyIndex1 == 3 || _BodyIndex1 == 13) //4 panel , Double Layer Circular Inner Skin Bag
            {
                if (textBodyL.Text == textBodyW.Text)
                {

                    if (_Type == 0) //Internal
                    {
                        InnerSkinFabricSize = _BagLenght + 12;
                        InnerSkinCutLenght = _BagHeight + 12 + InnerSkinExtraCutLenght;
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * 4 * InnerSkinFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        InnerSkinFabricSize = (_BagLenght + 12);
                        InnerSkinCutLenght = (_BagHeight + 8) + InnerSkinExtraCutLenght;
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * 4 * InnerSkinFabricSize;
                    }
                }
                else
                {
                    if (_Type == 0) //Internal
                    {
                        InnerSkinFabricSize = _BagLenght + 12;
                        InnerSkinCutLenght = _BagHeight + 12 + InnerSkinExtraCutLenght;
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * 2 * InnerSkinFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        InnerSkinFabricSize = (_BagLenght + 12);
                        InnerSkinCutLenght = (_BagHeight + 8) + InnerSkinExtraCutLenght;
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * 2 * InnerSkinFabricSize;
                    }
                }
            }
            if (_BodyIndex1 == 0 || _BodyIndex1 == 12) // UPanel,Double Layer Tunnel Lift
            {
                if (comboBody2.SelectedIndex == 3) //Wider Fold
                {
                    if (_Type == 0) //Internal
                    {
                        InnerSkinFabricSize = (_BagLenght + 15);
                        InnerSkinCutLenght = ((_BagHeight * 2) + _BagWidth + 14) + InnerSkinExtraCutLenght;
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        InnerSkinFabricSize = (_BagLenght + 15);
                        InnerSkinCutLenght = (_BagHeight * 2) + _BagWidth + 8 + InnerSkinExtraCutLenght;
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;
                    }
                }

                else if (comboBody3.SelectedIndex == 1 || comboBody3.SelectedIndex == 3) //UN with FS/DS 11.08.2021 UN+FDA
                {
                    if (_Type == 0) //Internal
                    {
                        InnerSkinFabricSize = (_BagLenght + 15);
                        InnerSkinCutLenght = ((_BagHeight * 2) + _BagWidth + 19) + InnerSkinExtraCutLenght;
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        InnerSkinFabricSize = (_BagLenght + 15);
                        InnerSkinCutLenght = (_BagHeight * 2) + _BagWidth + 8 + InnerSkinExtraCutLenght;
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;
                    }
                }
                else if (comboBody3.SelectedIndex == 0)  //Std
                {
                    if (_Type == 0) //Internal
                    {
                        InnerSkinFabricSize = (_BagLenght + 12);
                        InnerSkinCutLenght = ((_BagHeight * 2) + _BagWidth + 14) + InnerSkinExtraCutLenght;
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        InnerSkinFabricSize = (_BagLenght + 12);
                        InnerSkinCutLenght = (_BagHeight * 2) + _BagWidth + 6;
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;
                    }
                }
                if (comboLoopConst.SelectedIndex == 3) //Full Loop + Cross Corner
                {
                    if (_Type == 0) //Internal
                    {
                        InnerSkinFabricSize = (_BagLenght + 12);
                        InnerSkinCutLenght = ((_BagHeight * 2) + _BagWidth + 14);
                        InnerSkinWt = (InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize);
                        ///  + .1 * (InnerSkinCutLenght * (Utility.SafeConvertToDouble (comboInnerSkinGSM.Text) + Utility.SafeConvertToDouble (comboInnerSkinLamiGSM.Text)) * InnerSkinFabricSize);
                    }
                    else if (_Type == 1) //External
                    {

                        InnerSkinFabricSize = (_BagLenght + 12);
                        InnerSkinCutLenght = (_BagHeight * 2) + _BagWidth + 6;
                        InnerSkinWt = (InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize);
                        ///  + .1 * (InnerSkinCutLenght * (Utility.SafeConvertToDouble (comboInnerSkinGSM.Text) + Utility.SafeConvertToDouble (comboInnerSkinLamiGSM.Text)) * InnerSkinFabricSize);
                    }
                }
            }


            if (_BodyIndex1 == 1) // Circular
            {
                if (_Type == 0) //Internal
                {

                    InnerSkinFabricSize = _BagLenght + _BagWidth;
                    InnerSkinCutLenght = (_BagHeight + 12);
                    InnerSkinWt = (InnerSkinCutLenght * InnerSkinFabricSize * 2 * (InnerSkinGSM + InnerSkinLamiGSM));
                    ///     + .1111 * (InnerSkinCutLenght * InnerSkinFabricSize * 2 * (Utility.SafeConvertToDouble (comboInnerSkinGSM.Text) + Utility.SafeConvertToDouble (comboInnerSkinLamiGSM.Text)));
                }
                else
                {
                    InnerSkinFabricSize = _BagLenght + _BagWidth;
                    InnerSkinCutLenght = (_BagHeight + 8);
                    InnerSkinWt = (InnerSkinCutLenght * InnerSkinFabricSize * 2 * (InnerSkinGSM + InnerSkinLamiGSM));
                    ///+ .1111 * (InnerSkinCutLenght * InnerSkinFabricSize * 2 * (Utility.SafeConvertToDouble (comboInnerSkinGSM.Text) + Utility.SafeConvertToDouble (comboInnerSkinLamiGSM.Text)));

                }
            }

            if (_BodyIndex1 == 2) //Buffle
            {
                if (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 6 || comboBuffleType.SelectedIndex == 1) //4 Side Buffle,Middle Seam
                {
                    if (textBodyW.Text == textBodyL.Text)
                    {
                        if (_Type == 0) //Internal
                        {
                            InnerSkinFabricSize = _BagLenght + 12;
                            InnerSkinCutLenght = _BagHeight + 12;
                            InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * 4 * InnerSkinFabricSize;
                        }
                        else if (_Type == 1) //External
                        {
                            InnerSkinFabricSize = (_BagLenght + 12);
                            InnerSkinCutLenght = (_BagHeight + 8);
                            InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * 4 * InnerSkinFabricSize;
                        }
                    }
                    else
                    {
                        if (_Type == 0) //Internal
                        {
                            InnerSkinFabricSize = _BagLenght + 12;
                            InnerSkinCutLenght = _BagHeight + 12;
                            InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * 2 * InnerSkinFabricSize;
                        }
                        else if (_Type == 1) //External
                        {
                            InnerSkinFabricSize = (_BagLenght + 12);
                            InnerSkinCutLenght = (_BagHeight + 8);
                            InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * 2 * InnerSkinFabricSize;
                        }
                    }
                }

                else if (comboBuffleType.SelectedIndex == 2 || comboBuffleType.SelectedIndex == 3) //Tube + Corner + Buffle
                {
                    if (_Type == 0) //Internal
                    {
                        InnerSkinFabricSize = _BagLenght + _BagWidth + 8;
                        InnerSkinCutLenght = (2 * InnerSkinFabricSize) * (_BagHeight + 12);
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM);
                    }
                    else
                    {
                        InnerSkinFabricSize = _BagLenght + _BagWidth;
                        InnerSkinCutLenght = (2 * InnerSkinFabricSize) * (_BagHeight + 8);
                        InnerSkinWt = (InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM));
                    }
                }
                else if (comboBuffleType.SelectedIndex == 4) // 2Panel Corner + Buffle
                {
                    if (_Type == 0) //Internal
                    {
                        InnerSkinFabricSize = _BagLenght + _BagWidth + 20;
                        InnerSkinCutLenght = (2 * InnerSkinFabricSize) * (_BagHeight + 12);
                        InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM);
                    }
                    else //External
                    {
                        InnerSkinFabricSize = _BagLenght + _BagWidth + 16;
                        InnerSkinCutLenght = (2 * InnerSkinFabricSize) * (_BagHeight + 8);
                        InnerSkinWt = (InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM));
                    }
                }
                else if (comboBuffleType.SelectedIndex == 5) // 2Panel + Cross Corner + Buffle
                {
                    if (_Type == 0) //Internal
                    {
                        InnerSkinFabricSize = _BagLenght + _BagWidth + 12;
                        InnerSkinCutLenght = (2 * InnerSkinFabricSize) * (_BagHeight + 12);
                        InnerSkinWt = (InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM));
                        ///+ .1111 * (InnerSkinCutLenght * (Utility.SafeConvertToDouble (comboInnerSkinGSM.Text) + Utility.SafeConvertToDouble (comboInnerSkinLamiGSM.Text)));
                    }
                    else //External
                    {
                        InnerSkinFabricSize = _BagLenght + _BagWidth + 8;
                        InnerSkinCutLenght = (2 * InnerSkinFabricSize) * (_BagHeight + 8);
                        InnerSkinWt = (InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM));
                        ///        + .1111 * (InnerSkinCutLenght * (Utility.SafeConvertToDouble (comboInnerSkinGSM.Text) + Utility.SafeConvertToDouble (comboInnerSkinLamiGSM.Text)));
                    }

                }
            }


            if (_BodyIndex1 == 4) // Tube + Corner
            {
                if (_Type == 0) //Internal
                {
                    InnerSkinFabricSize = (_BagLenght + 4 + _BagWidth + 4);

                    InnerSkinCutLenght = (2 * InnerSkinFabricSize) * (_BagHeight + 12);
                    InnerSkinWt = (InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM));
                    //     + .1 * (InnerSkinCutLenght * (Utility.SafeConvertToDouble (comboInnerSkinGSM.Text) + Utility.SafeConvertToDouble (comboInnerSkinLamiGSM.Text)));
                }
                else if (_Type == 1) //External
                {
                    InnerSkinFabricSize = (_BagLenght + 4 + _BagWidth + 4);
                    InnerSkinCutLenght = (2 * InnerSkinFabricSize) * (_BagHeight + 8);
                    InnerSkinWt = (InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM));
                    //   + .1 * (InnerSkinCutLenght * (Utility.SafeConvertToDouble (comboInnerSkinGSM.Text) + Utility.SafeConvertToDouble (comboInnerSkinLamiGSM.Text)));
                }
            }

            if (_BodyIndex1 == 5 || _BodyIndex1 == 7) // Single Loop ,SingleLoop+ 4 Side
            {
                double TotalBodyHt = 0;
                if (_Type == 0) //internal
                {

                    if (textSlitHt.Text != "")
                        SlitHt = (Math.Sqrt(((_BagLenght) * (_BagLenght)) +
                                      (_BagWidth) * (_BagWidth))) / 2;
                    else
                        SlitHt = Utility.SafeConvertToDouble(textSlitHt.Text);
                    SlitHt = Math.Round(SlitHt, 2);
                    textSlitHt.Text = SlitHt.ToString();
                    TotalBodyHt = SlitHt + Utility.SafeConvertToDouble(textFillHt.Text) + 12;

                }
                else //External
                {


                    if (textSlitHt.Text != "")
                        SlitHt = (Math.Sqrt(((_BagLenght) * (_BagLenght)) +
                                      (_BagWidth) * (_BagWidth))) / 2;
                    else
                        SlitHt = Utility.SafeConvertToDouble(textSlitHt.Text);

                    SlitHt = Math.Round(SlitHt, 2);
                    textSlitHt.Text = SlitHt.ToString();
                    TotalBodyHt = SlitHt + Utility.SafeConvertToDouble(textFillHt.Text) + 8;
                }


                if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                    TotalBodyHt = TotalBodyHt + 10;
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) > 1000 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250)
                    TotalBodyHt = TotalBodyHt + 15;
                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) > 1000 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                    TotalBodyHt = TotalBodyHt + 15;
                else
                    TotalBodyHt = TotalBodyHt + 20;

                if (comboBoxbottomsubtype.SelectedIndex == 4) //StarBased
                {
                    if (_BagLenght > _BagWidth)
                        TotalBodyHt += (_BagLenght) / 2;
                    else
                        TotalBodyHt += (_BagWidth) / 2;
                }
                InnerSkinFabricSize = (_BagLenght + _BagWidth);
                InnerSkinCutLenght = TotalBodyHt;
                InnerSkinWt = 2 * InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;

                TotalHt = TotalBodyHt;
            }


            if (_BodyIndex1 == 6 || _BodyIndex1 == 8) // Double Loop 
            {
                double TotalBodyHt = 0;

                if (_BagLenght > _BagWidth)
                    SlitHt = (_BagLenght) / 2;
                else
                    SlitHt = (_BagWidth) / 2;

                if (_Type == 0) //Internal
                    TotalBodyHt = SlitHt + _BagHeight + 12;
                else
                    TotalBodyHt = SlitHt + _BagHeight + 8;

                if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                    TotalBodyHt = TotalBodyHt + 10;
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) > 1000 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250)
                    TotalBodyHt = TotalBodyHt + 15;
                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) > 1000 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                    TotalBodyHt = TotalBodyHt + 15;
                else
                    TotalBodyHt = TotalBodyHt + 20;

                if (comboBoxbottomsubtype.SelectedIndex == 4) //StarBased
                {
                    if (_BagLenght > _BagWidth)
                        TotalBodyHt += (_BagLenght) / 2;
                    else
                        TotalBodyHt += (_BagWidth) / 2;
                }

                InnerSkinFabricSize = (_BagLenght + _BagWidth);
                InnerSkinCutLenght = TotalBodyHt;
                InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize * 2;
                TotalHt = TotalBodyHt;
            }

            if (_BodyIndex1 == 9)  // Conical Bag Three Piece
            {
                if (_Type == 0) //Internal
                {
                    InnerSkinFabricSize = _BagLenght + 12;
                    InnerSkinCutLenght = _BagHeight + 12;
                    InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * 4 * InnerSkinFabricSize;
                }
                else if (_Type == 1) //External
                {
                    InnerSkinFabricSize = (_BagLenght + 12);
                    InnerSkinCutLenght = (_BagHeight + 8);
                    InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * 4 * InnerSkinFabricSize;
                }
            }

            if (_BodyIndex1 == 10)  // Conical Bag Single Piece
            {
                double OneSideDia = (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) * 3.14) / 4;
                if (_Type == 0) //internal
                {
                    InnerSkinFabricSize = (_BagLenght + 12);
                    InnerSkinCutLenght = ((_BagWidth - OneSideDia) / 2) + 14 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + _BagHeight;
                    InnerSkinWt = InnerSkinCutLenght * (_BagGSM + InnerSkinLamiGSM) * InnerSkinFabricSize * 4;
                }
                else
                {
                    InnerSkinFabricSize = (_BagLenght + 12);
                    InnerSkinCutLenght = ((_BagWidth - OneSideDia) / 2) + 10 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + _BagHeight;
                    InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize * 4;
                }
            }

            if (_BodyIndex1 == 11)  // Hood Bag/Covered Bag
            {
                if (_Type == 0) //internal
                {
                    InnerSkinFabricSize = (_BagLenght + 12);
                    InnerSkinCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(comboHoodSIze.Text) + 14;
                    InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;
                }
                else
                {
                    InnerSkinFabricSize = (_BagLenght + 12);
                    InnerSkinCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(comboHoodSIze.Text) + 8;
                    InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;
                }
            }

            if (_BodyIndex1 == 12) // Double Layer Tunnel Lift Bag
            {
                if (_Type == 0) //internal
                {
                    // InnerSkinFabricSize = (_BagWidth + 8);
                    InnerSkinCutLenght = (_BagHeight * 2) + _BagLenght + 160;

                }
                else
                {
                    //InnerSkinFabricSize = (_BagWidth + 8);
                    InnerSkinCutLenght = (_BagHeight * 2) + _BagLenght + 150;
                    //  InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;
                }
                InnerSkinFabricSize = (_BagWidth + 8);
                InnerSkinWt = InnerSkinCutLenght * (InnerSkinGSM + InnerSkinLamiGSM) * InnerSkinFabricSize;

            }
            if (checkBoxRF.Checked)
                InnerSkinWt += InnerSkinWt * 0.1111;
        }
        private void TopBandWtFormula()
        {
            TopBandFabricSize = Utility.SafeConvertToDouble(comboTopBandSize.Text);
            if (_Type == 0) //Internal
                TopBandCutLenght = ((_BagLenght + _BagWidth) * 2) + 20;
            else
                TopBandCutLenght = ((_BagLenght + _BagWidth) * 2) + 15;

            TopBandTotalMtr = ((TopBandCutLenght * _BagQty) / 100);
                            // + .1 * ((TopBandCutLenght * _BagQty) / 100);
            TopBandWt = ((Utility.SafeConvertToDouble(combotopbandgrm.Text) * TopBandCutLenght) / 100000);
        }

        /// <summary>
        /// TopBandBellyBand1WtFormula
        /// 18.06.2021
        /// </summary>
        private void TopBandBellyBand1WtFormula()
        {
            TopBellyBand1FabricSize = Utility.SafeConvertToDouble(comboTopBellyBand1Size.Text);
            if (_Type == 0) //Internal
                TopBellyBand1CutLenght = ((_BagLenght + _BagWidth) * 2) + 20;
            else
                TopBellyBand1CutLenght = ((_BagLenght + _BagWidth) * 2) + 15;

            TopBellyBand1TotalMtr = ((TopBellyBand1CutLenght * _BagQty) / 100);
                            // + .1 * ((TopBellyBand1CutLenght * _BagQty) / 100);
            TopBellyBand1Wt = ((Utility.SafeConvertToDouble(combotopBellyband1grm.Text) * TopBellyBand1CutLenght) / 100000);
        }
        /// <summary>
        /// TopBandBellyBand2WtFormula
        /// 18.06.2021
        /// </summary>
        private void TopBandBellyBand2WtFormula()
        {
            TopBellyBand2FabricSize = Utility.SafeConvertToDouble(comboTopBellyBand2Size.Text);
            if (_Type == 0) //Internal
                TopBellyBand2CutLenght = ((_BagLenght + _BagWidth) * 2) + 20;
            else
                TopBellyBand2CutLenght = ((_BagLenght + _BagWidth) * 2) + 15;

            TopBellyBand2TotalMtr = ((TopBellyBand2CutLenght * _BagQty) / 100);
                            // + .1 * ((TopBellyBand2CutLenght * _BagQty) / 100);
            TopBellyBand2Wt = ((Utility.SafeConvertToDouble(combotopBellyband2grm.Text) * TopBellyBand2CutLenght) / 100000);
        }
        /// <summary>
        /// TopBttomBandWtFormula
        /// 18.06.2021
        /// </summary>
        private void TopBttomBandWtFormula()
        {
            TopBottomBandFabricSize = Utility.SafeConvertToDouble(comboTopBottomBandSize.Text);
            if (_Type == 0) //Internal
                TopBottomBandCutLenght = ((_BagLenght + _BagWidth) * 2) + 20;
            else
                TopBottomBandCutLenght = ((_BagLenght + _BagWidth) * 2) + 15;

            TopBottomBandTotalMtr = ((TopBottomBandCutLenght * _BagQty) / 100);
                           //  + .1 * ((TopBottomBandCutLenght * _BagQty) / 100);
            TopBottomBandWt = ((Utility.SafeConvertToDouble(combotopBottomBandgrm.Text) * TopBottomBandTotalMtr) / 100000);
        }
        private void InnerBoxWtFormula()
        {
            if (_Type == 0) //Internal
            {
                InnerBoxFabricSize = _BagLenght + _BagWidth + 4;
                InnerBoxCutLenght = _BagWidth + 17;
            }
            else
            {
                InnerBoxFabricSize = _BagLenght + _BagWidth - 6;
                InnerBoxCutLenght = _BagWidth + 12;
            }
            InnerBoxWt = InnerBoxFabricSize * InnerBoxCutLenght * 2
    * (Utility.SafeConvertToDouble(comboInnerBoxGSM.Text) + Utility.SafeConvertToDouble(comboInnerBoxLamiGSM.Text));

            InnerBoxWt = InnerBoxWt / 10000000;
            InnerBoxWt = Math.Round(InnerBoxWt, 4);

            InnerBoxTotalMtr = (_BagQty * (InnerBoxCutLenght / 100)); 
               //+  .1 * (_BagQty * (InnerBoxCutLenght / 100)));
            InnerBoxTotalMtr = Math.Round(InnerBoxTotalMtr, 2);
        }
        private double LabelWtFormula()
        {
            if (checkTyvac.Checked)
            {
                if (comboLabelMicron.Text.Length == 0)
                    comboLabelMicron.Text = "100";
                LabelWt = Utility.SafeConvertToDouble(textLabelL.Text) * Utility.SafeConvertToDouble(textLabelW.Text)
                         * Utility.SafeConvertToDouble(comboLabelMicron.Text) * 2.54 * 2.54;
            }
            else
            {
                if (comboLabelMicron.Text.Length == 0)
                    comboLabelMicron.Text = "100";
                LabelWt = Utility.SafeConvertToDouble(textLabelL.Text) * Utility.SafeConvertToDouble(textLabelW.Text)
                         * Utility.SafeConvertToDouble(comboLabelMicron.Text) * 2.54 * 2.54 * .92;
            }
            return LabelWt;
        }
        #region event
        private void checkBoxTop_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTop.Checked)
                groupTop.Visible = true;
            else
                groupTop.Visible = false;
            groupSpout.Visible = false;
            groupBox6.Visible = false;
            groupBoxduffleskirt.Visible = false;
        }
        private void checkStevdore_CheckedChanged(object sender, EventArgs e)
        {
            comboStPortion.SelectedIndex = 0;
            if (checkStevdore.Checked)
                groupBoxstevdore.Visible = true;
            else
                groupBoxstevdore.Visible = false;
        }
        private void checkBoxTunnel_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTunnel.Checked)
            {
                comboTunnelDesign.SelectedIndex = 0;
                groupTunnel.Visible = true;
            }
            else
                groupTunnel.Visible = false;
        }
        private void checkboxtopflap_CheckedChanged(object sender, EventArgs e)
        {
            if (checktopflap.Checked)
                groupBoxtopflap.Visible = true;
            else
                groupBoxtopflap.Visible = false;
        }
        private void checkBoxTopBand_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTopBand.Checked)
                groupBoxtopband.Visible = true;
            else
                groupBoxtopband.Visible = false;
        }
        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (checkbottom.Checked)
                groupboxbottom.Visible = true;
            else
                groupboxbottom.Visible = false;

            if (_BodyIndex1 == 1 && IsupdateMode == false) //Circular
                comboBoxbottomgsm.Text = Convert.ToString(Convert.ToInt32(comboBodyGSM.Text) + 10);

            else if ((_BodyIndex1 == 0 || _BodyIndex1 == 2 || _BodyIndex1 == 3) && IsupdateMode == false)
                comboBoxbottomgsm.Text = comboBodyGSM.Text;
            else if(IsupdateMode == false)
                comboBoxbottomgsm.Text = "70";
        }
        private void checkboxbottomflap_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomflap.Checked)
                groupBoxbottomflap.Visible = true;
            else
                groupBoxbottomflap.Visible = false;
        }
        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxlinertype.SelectedIndex = 1;
            #region Checkbox Of Liner
            if (checkBoxliner.Checked)
            {
                groupBoxliner.Visible = true;
                comboBoxlinermicron.Text = "70";
                comboBoxlinertype1.Items.Clear();

                comboBoxlinertype1.Items.Add("None");
                comboBoxlinertype1.Items.Add("Form Fit Liner");
                comboBoxlinertype1.Items.Add("Form Fit Flenze Liner");
                comboBoxlinertype1.Items.Add("Gusseted Liner");
                comboBoxlinertype1.Items.Add("Suspended");
                comboBoxlinertype1.Items.Add("Tray Liner");


                comboBoxlinertype1.SelectedIndex = 0;
                //By Rikin on 20-Apr-2015 for Buffle seams calculation for liner
                if (_BodyIndex1 != 2)
                {
                    checkBoxlinerBuffle.Visible = true;
                    checkBoxlinerBuffle.Checked = false;
                }
                else
                {
                    checkBoxlinerBuffle.Visible = false;
                    checkBoxlinerBuffle.Checked = false;
                }

            }
            else
                groupBoxliner.Visible = false;
            #endregion

        }
        private void checkBoxlabel_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxlabel.Checked)
                groupBoxlabel.Visible = true;
            else
                groupBoxlabel.Visible = false;
        }
        private void checkBoxblock_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxblock.Checked)
                groupBoxblock.Visible = true;
            else
                groupBoxblock.Visible = false;
        }
        private void checkBoxdocpouch_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxdocpouch.Checked)
                groupBoxdocpouch.Visible = true;
            else
                groupBoxdocpouch.Visible = false;
        }
        private void comboTopType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsupdateMode == false)
                checkSpoutTie.Checked = false;// by Rikin on 10-MAr-2015

            if (comboTopType.SelectedIndex == 0)
            {
                groupBox6.Visible = false;
                groupSpout.Visible = false;
                groupBoxduffleskirt.Visible = false;
                checkSpoutRope.Visible = false;
            }
            else if (comboTopType.SelectedIndex == 1 || comboTopType.SelectedIndex == 4 || comboTopType.SelectedIndex == 3)
            {
                groupSpout.Visible = true;
                groupBox6.Visible = true;
                groupBoxduffleskirt.Visible = false;
                if (IsupdateMode == false)
                {
                    if (comboTopType.SelectedIndex == 1)
                    {
                        comboSpoutType.SelectedIndex = 1;
                        checkSpoutRope.Visible = true;
                    }
                    else
                    {
                        checkSpoutRope.Visible = false;
                        comboSpoutType.SelectedIndex = 0;
                    }
                }
            }
            else if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8) //17.06.2021
            {
                groupBox6.Visible = true;
                groupSpout.Visible = false;
                groupBoxduffleskirt.Visible = true;
                checkSpoutRope.Visible = false;
                if (IsupdateMode == false)
                    comboBoxduffleskirtheight.Text = Convert.ToString(((_BagLenght + _BagWidth) / 2) - 10);

            }
            else if (comboTopType.SelectedIndex == 6 || comboTopType.SelectedIndex == 9)//RIkin for leno
            {
                groupBox6.Visible = true;
                groupSpout.Visible = false;
                groupBoxduffleskirt.Visible = true;
                checkSpoutRope.Visible = false;
                if (IsupdateMode == false)
                    comboBoxduffleskirtheight.Text = (Convert.ToString(((_BagWidth + _BagLenght) / 2) - 10));
            }
            if (comboTopType.SelectedIndex == 1 || comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 6 ||
                comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8 || comboTopType.SelectedIndex == 9) //17.06.2021
            {
                checkSpoutTie.Checked = true;// by Rikin on 10-MAr-2015
                if (IsupdateMode == false)
                {
                    comboBoxTopGSM.Text = "70";
                    comboTopLamiGSM.Text = "25";
                }
                //checkBoxTopLam.Checked = true;
                if (comboTopType.SelectedIndex == 1) // Spout
                {
                    if (IsupdateMode == false)
                    {
                        comboSpoutGSM.Text = "70";
                        comboSpoutLamiGSM.Text = "25";
                        comboSpoutDia.Text = "35";
                        comboSpoutHeight.Text = "50";
                        textTopRemarks.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboSpoutDia.Text) - 5);
                    }
                    checkBoxSpoutLam.Checked = true;

                }
                else
                {
                    comboSpoutGSM.Text = "0";
                    comboSpoutLamiGSM.Text = "0";
                    checkBoxSpoutLam.Checked = false;
                }
            }
            else
            {
                if (IsupdateMode == false)
                {
                    comboBoxTopGSM.Text = "0";
                    comboTopLamiGSM.Text = "0";
                    checkBoxTopLam.Checked = false;
                }
            }
            //  if (comboTopType.SelectedIndex == 1) //Top Spout

        }
        private void checkBoxbottomvelcro_CheckedChanged(object sender, EventArgs e)
        {
            if (checkbottomvelcro.Checked)
                combobottomvelcro.Enabled = true;
            else
                combobottomvelcro.Enabled = false;
        }
        #endregion

        private double DocWtFormula()
        {
            if (comboDocType1.SelectedIndex == 0 || comboDocType1.SelectedIndex == 2 || comboDocType1.SelectedIndex == 3)
            {
                DocFabricSize = Utility.SafeConvertToDouble(textDocL.Text) + 4;
                DocCutLenght = Utility.SafeConvertToDouble(textDocW.Text);
                if (comboDocMicron.Text.Length == 0)
                    comboDocMicron.Text = "100";
                if (comboDocUnit.SelectedIndex == 1) //Inch
                {
                    DocFabricSize = DocFabricSize * 2.54;
                    DocCutLenght = DocCutLenght * 2.54;
                    DocWt = DocFabricSize * DocCutLenght * 2 * Utility.SafeConvertToDouble(comboDocMicron.Text) * .92;
                }
                else
                    DocWt = DocFabricSize * DocCutLenght * 2 * Utility.SafeConvertToDouble(comboDocMicron.Text) * .92;
            }
            else
            {
                DocFabricSize = Utility.SafeConvertToDouble(textDocL.Text);
                DocCutLenght = Utility.SafeConvertToDouble(textDocW.Text) + 4;
                if (comboDocMicron.Text == "")
                    comboDocMicron.Text = "100";
                if (comboDocUnit.SelectedIndex == 1) //Inch
                {
                    DocFabricSize = DocFabricSize * 2.54;
                    DocCutLenght = DocCutLenght * 2.54;
                    DocWt = DocFabricSize * DocCutLenght * 2 * Utility.SafeConvertToDouble(comboDocMicron.Text) * .92;
                }
                else
                    DocWt = DocFabricSize * DocCutLenght * 2 * Utility.SafeConvertToDouble(comboDocMicron.Text) * .92;
            }
            if (comboDocType.SelectedIndex == 1) //Zip Lock
                DocWt = DocWt + DocWt * .16;

            DocWt = DocWt * Convert.ToInt32(textDocNo.Text);
            return DocWt;
        }


        private double Doc1WtFormula()
        {
            if (combodoctype4.SelectedIndex == 0 || combodoctype4.SelectedIndex == 2)
            {
                Doc1FabricSize = Utility.SafeConvertToDouble(textDoc1L.Text) + 4;
                Doc1CutLenght = Utility.SafeConvertToDouble(textDoc1W.Text);
                if (comboDoc1Micron.Text.Length == 0)
                    comboDoc1Micron.Text = "80";
                if (comboDoc1Unit.SelectedIndex == 1) //Inch
                {
                    Doc1FabricSize = Doc1FabricSize * 2.54;
                    Doc1CutLenght = Doc1CutLenght * 2.54;
                    Doc1Wt = Doc1FabricSize * Doc1CutLenght * 2 * Utility.SafeConvertToDouble(comboDoc1Micron.Text) * .92;
                }
                else
                    Doc1Wt = Doc1FabricSize * Doc1CutLenght * 2 * Utility.SafeConvertToDouble(comboDoc1Micron.Text) * .92;
            }
            else
            {
                Doc1FabricSize = Utility.SafeConvertToDouble(textDoc1L.Text);
                Doc1CutLenght = Utility.SafeConvertToDouble(textDoc1W.Text) + 4;
                if (comboDoc1Micron.Text == "")
                    comboDoc1Micron.Text = "80";
                if (comboDoc1Unit.SelectedIndex == 1) //Inch
                {
                    Doc1FabricSize = Doc1FabricSize * 2.54;
                    Doc1CutLenght = Doc1CutLenght * 2.54;
                    Doc1Wt = Doc1FabricSize * Doc1CutLenght * 2 * Utility.SafeConvertToDouble(comboDoc1Micron.Text) * .92;
                }
                else
                    Doc1Wt = Doc1FabricSize * Doc1CutLenght * 2 * Utility.SafeConvertToDouble(comboDoc1Micron.Text) * .92;
            }
            if (combodoctype3.SelectedIndex == 1) //Zip Lock
                Doc1Wt = Doc1Wt + Doc1Wt * .16;

            Doc1Wt = Doc1Wt * Convert.ToInt32(textdoc1No.Text);
            return Doc1Wt;
        }


        private double Doc2WtFormula()
        {
            if (combodoctype7.SelectedIndex == 0 || combodoctype7.SelectedIndex == 2)
            {
                Doc2FabricSize = Utility.SafeConvertToDouble(textDoc2L.Text) + 4;
                Doc2CutLenght = Utility.SafeConvertToDouble(textDoc2W.Text);
                if (comboDoc2Micron.Text.Length == 0)
                    comboDoc2Micron.Text = "80";
                if (comboDoc2Unit.SelectedIndex == 1) //Inch
                {
                    Doc2FabricSize = Doc2FabricSize * 2.54;
                    Doc2CutLenght = Doc2CutLenght * 2.54;
                    Doc2Wt = Doc2FabricSize * Doc2CutLenght * 2 * Utility.SafeConvertToDouble(comboDoc2Micron.Text) * .92;
                }
                else
                    Doc2Wt = Doc2FabricSize * Doc2CutLenght * 2 * Utility.SafeConvertToDouble(comboDoc2Micron.Text) * .92;
            }
            else
            {
                Doc2FabricSize = Utility.SafeConvertToDouble(textDoc2L.Text);
                Doc2CutLenght = Utility.SafeConvertToDouble(textDoc2W.Text) + 4;
                if (comboDoc2Micron.Text == "")
                    comboDoc2Micron.Text = "80";
                if (comboDoc2Unit.SelectedIndex == 1) //Inch
                {
                    Doc2FabricSize = Doc2FabricSize * 2.54;
                    Doc2CutLenght = Doc2CutLenght * 2.54;
                    Doc2Wt = Doc2FabricSize * Doc2CutLenght * 2 * Utility.SafeConvertToDouble(comboDoc2Micron.Text) * .92;
                }
                else
                    Doc2Wt = Doc2FabricSize * Doc2CutLenght * 2 * Utility.SafeConvertToDouble(comboDoc2Micron.Text) * .92;
            }
            if (combodoctype6.SelectedIndex == 1) //Zip Lock
                Doc2Wt = Doc2Wt + Doc2Wt * .16;

            Doc2Wt = Doc2Wt * Convert.ToInt32(textdoc2No.Text);
            return Doc2Wt;
        }


        private double FSTieFormula()
        {
            if (checkSpoutTie.Checked)
            {
                FSTieCutLenght = Utility.SafeConvertToDouble(comboSpoutTieCutSize.Text) * 2 + 5;
                FSTieFabricSize = Utility.SafeConvertToDouble(comboSpoutTieSize.Text);
                FSTieWt = FSTieCutLenght * Utility.SafeConvertToDouble(comboSpoutTieGrm.Text) * Utility.SafeConvertToDouble(textTopSpoutTieNo.Text);
            }
            return FSTieWt;
        }

        private double FSIRISTieFormula() //29.0.2021
        {
            if (checkIRISTie.Checked)
            {
                FSIRISTieCutLenght = Utility.SafeConvertToDouble(comboSpoutTieIRISCutSize.Text) * 2 + 5;
                FSIRISTieFabricSize = Utility.SafeConvertToDouble(comboSpoutTieIRISCutSize.Text);
                FSIRISTieWt = FSIRISTieCutLenght * Utility.SafeConvertToDouble(comboSpoutTieIRISGrm.Text) * Utility.SafeConvertToDouble(textTopSpoutTieIRISNo.Text);
            }
            return FSTieWt;
        }

        private double DSTieFormula()
        {
            if (checkBottomSpoutTie.Checked)
            {
                DSTieCutLenght = Utility.SafeConvertToDouble(comboBottomSpoutTieCutSize.Text) * 2 + 5;
                DSTieFabricSize = Utility.SafeConvertToDouble(comboBottomSpoutTieSize.Text);
                DSTieWt = DSTieCutLenght * Utility.SafeConvertToDouble(comboBottomSpoutTieGrm.Text) * Utility.SafeConvertToDouble(textBottomSpoutTieNo.Text);
            }
            return DSTieWt;
        }
        private double DSIRISTieFormula() //29.09.2021
        {
            if (checkBottomspoutiristie.Checked)
            {
                DSIRISTieCutLenght = Utility.SafeConvertToDouble(comboBottomSpoutTieIRISCutSize.Text) * 2 + 5;
                DSIRISTieFabricSize = Utility.SafeConvertToDouble(comboBottomSpoutTieIRISSize.Text);
                DSIRISTieWt = DSTieCutLenght * Utility.SafeConvertToDouble(comboBottomSpoutTieIRISGrm.Text) * Utility.SafeConvertToDouble(textBottomSpoutTieIRISNo.Text);
            }
            return DSIRISTieWt;
        }
        private double DSTieFormula1()
        {
            if (checkBottomSpoutTie1.Checked)
            {
                DSTieCutLenght1 = Utility.SafeConvertToDouble(comboBottomSpoutTieCutSize1.Text) * 2 + 5;
                DSTieFabricSize1 = Utility.SafeConvertToDouble(comboBottomSpoutTieSize1.Text);
                DSTieWt1 = DSTieCutLenght1 * Utility.SafeConvertToDouble(comboBottomSpoutTieGrm1.Text) * Utility.SafeConvertToDouble(textBottomSpoutTieNo1.Text);
            }
            return DSTieWt1;
        }
        private double DSTieFormula2()
        {
            if (checkBottomSpoutTie2.Checked)
            {
                DSTieCutLenght2 = Utility.SafeConvertToDouble(comboBottomSpoutTieCutSize2.Text) * 2 + 5;
                DSTieFabricSize2 = Utility.SafeConvertToDouble(comboBottomSpoutTieSize2.Text);
                DSTieWt2 = DSTieCutLenght2 * Utility.SafeConvertToDouble(comboBottomSpoutTieGrm2.Text) * Utility.SafeConvertToDouble(textBottomSpoutTieNo2.Text);
            }
            return DSTieWt2;
        }
        private double TopTieFormula()
        {
            if (checkTopTie.Checked)
            {
                double Circumference = 3.14 * Utility.SafeConvertToDouble(comboSpoutDia.Text);
                if (comboSpoutType.SelectedIndex == 2 || comboSpoutType.SelectedIndex == 4 || comboSpoutType.SelectedIndex == 6)
                    TopTieCutLenght = Circumference + 35;
                else
                    TopTieCutLenght = Utility.SafeConvertToDouble(comboTopTieCutSizes.Text) * 2 + 5;

                TopTieFabricSize = Utility.SafeConvertToDouble(comboTopTieSizes.Text);
                TopTieWt = TopTieCutLenght * Utility.SafeConvertToDouble(comboTopTieGrms.Text) * Utility.SafeConvertToDouble(textTopTieNo.Text);
            }
            return TopTieWt;
        }
        private double BottomTieFormula()
        {
            if (checkBottomTie.Checked)
            {
                if (comboBoxbottomsubtype.SelectedIndex == 1 || comboBoxbottomsubtype.SelectedIndex == 2) // Petal & Iris Closure
                {
                    double Circumference = 3.14 * Utility.SafeConvertToDouble(comboBoxbottomdia.Text);
                    BottomTieCutLenght = Circumference + 35;
                }
                else
                    BottomTieCutLenght = Utility.SafeConvertToDouble(comboBottomTieCutSize.Text) * 2 + 5;

                BottomTieFabricSize = Utility.SafeConvertToDouble(comboBottomTieSize.Text);
                BottomTieWt = BottomTieCutLenght * Utility.SafeConvertToDouble(comboBottomTieGrm.Text) * Utility.SafeConvertToDouble(textBottomTieNo.Text);
            }
            return BottomTieWt;
        }


        private double BottomLoopFormula()
        {
            if (checkBottomloop.Checked)
            {
                if (textBottomLoopLenght.Text == "")
                    textBottomLoopLenght.Text = "0";
                BottomLoopLenght = Utility.SafeConvertToDouble(textBottomLoopLenght.Text);

                if (textBottomLoopFabricSize.Text == "")
                    textBottomLoopFabricSize.Text = "0";
                BottomLoopSize = Utility.SafeConvertToDouble(textBottomLoopFabricSize.Text);
                BottomLoopWt = BottomLoopLenght * Utility.SafeConvertToDouble(comboBottomLoopgrm.Text) * Utility.SafeConvertToDouble(textBottomLoopNo.Text);
            }
            return BottomLoopWt;
        }

        private double TopHookFormula()
        {
            if (checkTopFlapHook.Checked)
            {
                if (comboTopFlapHookCutSize.Text == "0" || comboTopFlapHookCutSize.Text.Length == 0)
                    TopHookCutLenght = 20;
                else
                    TopHookCutLenght = Utility.SafeConvertToDouble(comboTopFlapHookCutSize.Text) * 2 + 5;

                TopHookFabricSize = Utility.SafeConvertToDouble(comboTopflapHookSize.Text);
                TopHookWt = TopHookCutLenght * Utility.SafeConvertToDouble(comboTopflapHookGrm.Text) * Utility.SafeConvertToDouble(textTopHookNo.Text);
            }
            return TopHookWt;
        }
        private double BottomHookFormula()
        {
            if (checkBottomFlapHook.Checked)
            {
                if (comboBottomFlapHookCutsize.Text == "0" || comboBottomFlapHookCutsize.Text.Length == 0)
                    BottomHookCutLenght = 20;
                else
                    BottomHookCutLenght = Utility.SafeConvertToDouble(comboTopFlapHookCutSize.Text) * 2 + 5;
                BottomHookFabricSize = Utility.SafeConvertToDouble(comboBottomFlapHookSize.Text);
                BottomHookWt = BottomHookCutLenght * Utility.SafeConvertToDouble(comboBottomFlapHookGrm.Text)
                     * Utility.SafeConvertToDouble(textBottomHookNo.Text);
            }
            return BottomHookWt;
        }
        private double TopRopeWtFormula()
        {
            if (checkTopRope.Checked)
            {
                TopRopeCutLenght = Utility.SafeConvertToDouble(comboTopRopeSizes.Text) * 20 + 5;
                TopRopeFabricSize = Utility.SafeConvertToDouble(comboTopRopeSizes.Text);
                TopRopeWt = TopRopeCutLenght * Utility.SafeConvertToDouble(comboTopRopeGrms.Text) * Utility.SafeConvertToDouble(textTopRopeNo.Text);
            }
            return TopRopeWt;
        }
        private double BottomRopeWtFormula()
        {
            if (checkBottomRope.Checked)
            {
                //BottomRopeCutLenght = Utility.SafeConvertToDouble(comboBottomRopeSizes.Text) * 20 + 5;
                BottomRopeCutLenght = Utility.SafeConvertToDouble(comboBottomRopeCutSizes.Text) +5;
               
                BottomRopeFabricSize = Utility.SafeConvertToDouble(comboBottomRopeSizes.Text);
                BottomRopeWt = BottomRopeCutLenght * Utility.SafeConvertToDouble(comboBottomRopeGrms.Text) * Utility.SafeConvertToDouble(textBottomRopeNo.Text);
            }
            return BottomRopeWt;
        }
        private double TopSpoutRopeWtFormula()
        {
            double Circumference = 3.14 * Utility.SafeConvertToDouble(comboSpoutDia.Text);
            if (comboTopType.SelectedIndex == 1 && comboSpoutType.Text != "") //Top Spout 
            {
                if (comboSpoutType.SelectedIndex == 1 || comboSpoutType.SelectedIndex == 2
                     || comboSpoutType.SelectedIndex == 4 || comboSpoutType.SelectedIndex == 6)
                {
                    if (comboSpoutType.SelectedIndex == 2)
                        TopSpoutRopeCutLenght = Circumference + 35;
                    else
                        TopSpoutRopeCutLenght = Circumference + 25;
                    //TopSpoutRopeFabricSize = Utility.SafeConvertToDouble(comboSpoutRopeSize.Text);
                    //TopSpoutRopeWt = TopSpoutRopeCutLenght * Utility.SafeConvertToDouble(comboSpoutGSM.Text) * Utility.SafeConvertToDouble(textTopSpoutRopeNo.Text);
                }
                else
                {
                    TopSpoutRopeCutLenght = Utility.SafeConvertToDouble(comboSpoutRopeSize.Text) * 2 + 5;
                }
                TopSpoutRopeFabricSize = Utility.SafeConvertToDouble(comboSpoutRopeSize.Text);
                TopSpoutRopeWt = TopSpoutRopeCutLenght * Utility.SafeConvertToDouble(comboTopSpoutRopeGrm.Text) * Utility.SafeConvertToDouble(textTopSpoutRopeNo.Text);
                if (comboSpoutType.SelectedIndex == 2)// By Rikin For Petal Flap Calculation
                {
                    TopPetalSize = Utility.SafeConvertToDouble(comboSpoutDia.Text) - 10;
                    TopPetalCutLength = TopPetalSize;
                    //Added 30 GSM for Unlaminated Bags on 09-Mar-2015
                    TopPetalWT = TopPetalCutLength * TopPetalSize * (Utility.SafeConvertToDouble(comboTopPetalFlapGSM.Text) + (checkPetalFlapGSMLam.Checked ? Utility.SafeConvertToDouble(comboTopPetalFlapGSMLam.Text) : 0));
                }
            }
            return TopSpoutRopeWt;
        }
        private double BottomSpoutRopeWtFormula()
        {
            double Circumference = 3.14 * Utility.SafeConvertToDouble(comboBoxbottomdia.Text);
            if (comboBoxbottomtype.SelectedIndex == 3 && comboBottomSpoutRope.Text != "") //Bottom Spout 
            {
                if (comboBoxbottomsubtype.SelectedIndex == 1 || comboBoxbottomsubtype.SelectedIndex == 2) // Petal & Iris Closure
                {
                    BottomSpoutRopeCutLenght = Circumference + 35;
                    BottomSpoutRopeFabricSize = Utility.SafeConvertToDouble(comboBottomSpoutRopeSize.Text);
                    BottomSpoutRopeWt = BottomSpoutRopeCutLenght * Utility.SafeConvertToDouble(comboBottomSpoutRopeGrm.Text) * Utility.SafeConvertToDouble(textBottomSpoutRopeNo.Text);
                    if (comboBoxbottomsubtype.SelectedIndex == 1)// By Rikin For Petal Flap Calculation
                    {
                        PetalSize = Utility.SafeConvertToDouble(comboBoxbottomdia.Text) - 10;
                        PetalCutLength = PetalSize;
                        //Added 30 GSM for Unlaminated Bags on 09-Mar-2015
                        PetalWT = PetalCutLength * PetalSize * (Utility.SafeConvertToDouble(comboBottomPetalFlapGSM.Text)
                            + (checkbottomPetalFlapGSM.Checked ? Utility.SafeConvertToDouble(combobottomPetalFlapGSMLam.Text) : 0)); // change by manish 22nd July 2022
                    }
                }
                else
                {
                    BottomSpoutRopeCutLenght = Utility.SafeConvertToDouble(comboBottomSpoutRopeSize.Text) * 2 + 5;
                    BottomSpoutRopeFabricSize = Utility.SafeConvertToDouble(comboBottomSpoutRopeSize.Text);
                    BottomSpoutRopeWt = BottomSpoutRopeCutLenght * Utility.SafeConvertToDouble(comboBottomSpoutRopeGrm.Text) * Utility.SafeConvertToDouble(textBottomSpoutRopeNo.Text);
                }
            }
            return BottomSpoutRopeWt;
        }


        private double BottomSpoutRopeWtFormula1()
        {
            double Circumference = 3.14 * Utility.SafeConvertToDouble(comboBoxbottomdia1.Text);
            if (comboBoxbottomtype1.SelectedIndex == 3 && comboBottomSpoutRope1.Text != "") //Bottom Spout 
            {
                if (comboBoxbottomsubtype1.SelectedIndex == 1 || comboBoxbottomsubtype1.SelectedIndex == 2) // Petal & Iris Closure
                {
                    BottomSpoutRopeCutLenght1 = Circumference + 35;
                    BottomSpoutRopeFabricSize1 = Utility.SafeConvertToDouble(comboBottomSpoutRopeSize1.Text);
                    BottomSpoutRopeWt1 = BottomSpoutRopeCutLenght1 * Utility.SafeConvertToDouble(comboBottomSpoutRopeGrm1.Text) * Utility.SafeConvertToDouble(textBottomSpoutRopeNo1.Text);
                    if (comboBoxbottomsubtype.SelectedIndex == 1)// By Rikin For Petal Flap Calculation
                    {
                        PetalSize = Utility.SafeConvertToDouble(comboBoxbottomdia.Text) - 10;
                        PetalCutLength = PetalSize;
                        //Added 30 GSM for Unlaminated Bags on 09-Mar-2015
                        PetalWT = PetalCutLength * PetalSize * (_BagGSM + (_BagLamiGSM > 0 ? _BagLamiGSM : 30));
                    }
                }
                else
                {
                    BottomSpoutRopeCutLenght1 = Utility.SafeConvertToDouble(comboBottomSpoutRopeSize1.Text) * 2 + 5;
                    BottomSpoutRopeFabricSize1 = Utility.SafeConvertToDouble(comboBottomSpoutRopeSize1.Text);
                    BottomSpoutRopeWt1 = BottomSpoutRopeCutLenght1 * Utility.SafeConvertToDouble(comboBottomSpoutRopeGrm1.Text) * Utility.SafeConvertToDouble(textBottomSpoutRopeNo1.Text);
                }
            }
            return BottomSpoutRopeWt1;
        }

        private double BottomSpoutRopeWtFormula2()
        {
            double Circumference = 3.14 * Utility.SafeConvertToDouble(comboBoxbottomdia2.Text);
            if (comboBoxbottomtype2.SelectedIndex == 3 && comboBottomSpoutRope2.Text != "") //Bottom Spout 
            {
                if (comboBoxbottomsubtype2.SelectedIndex == 1 || comboBoxbottomsubtype2.SelectedIndex == 2) // Petal & Iris Closure
                {
                    BottomSpoutRopeCutLenght2 = Circumference + 35;
                    BottomSpoutRopeFabricSize2 = Utility.SafeConvertToDouble(comboBottomSpoutRopeSize2.Text);
                    BottomSpoutRopeWt2 = BottomSpoutRopeCutLenght2 * Utility.SafeConvertToDouble(comboBottomSpoutRopeGrm2.Text) * Utility.SafeConvertToDouble(textBottomSpoutRopeNo2.Text);
                    if (comboBoxbottomsubtype.SelectedIndex == 1)// By Rikin For Petal Flap Calculation
                    {
                        PetalSize = Utility.SafeConvertToDouble(comboBoxbottomdia.Text) - 10;
                        PetalCutLength = PetalSize;
                        //Added 30 GSM for Unlaminated Bags on 09-Mar-2015
                        PetalWT = PetalCutLength * PetalSize * (_BagGSM + (_BagLamiGSM > 0 ? _BagLamiGSM : 30));
                    }
                }
                else
                {
                    BottomSpoutRopeCutLenght2 = Utility.SafeConvertToDouble(comboBottomSpoutRopeSize2.Text) * 2 + 5;
                    BottomSpoutRopeFabricSize2 = Utility.SafeConvertToDouble(comboBottomSpoutRopeSize2.Text);
                    BottomSpoutRopeWt2 = BottomSpoutRopeCutLenght2 * Utility.SafeConvertToDouble(comboBottomSpoutRopeGrm2.Text) * Utility.SafeConvertToDouble(textBottomSpoutRopeNo2.Text);
                }
            }
            return BottomSpoutRopeWt2;
        }


        private double BodyWtFormula()
        {


            if (_BodyIndex1 == 0 || _BodyIndex1 == 12) // UPanel,Double Layer Tunnel Lift
            {
                if (checkBoxTunnel.Checked)
                {
                    if (comboTunnelDesign.SelectedIndex == 0) //Flexcon
                    {
                        if (_Type == 1) //External
                        {
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = (_BagWidth + 18);
                                BodyCutLenght = ((_BagHeight * 2) + _BagLenght) + 140 + 18;
                            }
                            else
                            {
                                BodyFabricSize = (_BagWidth + 10);
                                BodyCutLenght = ((_BagHeight * 2) + _BagLenght) + 140;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                        if (_Type == 0) //internal
                        {
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = (_BagWidth + 18);
                                BodyCutLenght = ((_BagHeight + 18) * 2) + _BagLenght + 150 + 150;
                            }
                            else
                            {
                                BodyFabricSize = (_BagWidth + 15);
                                BodyCutLenght = (_BagHeight * 2) + _BagLenght + 150;
                            }
                            BodyWt = BodyCutLenght * Utility.SafeConvertToDouble(_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }

                        TunnelFabricSize = BodyFabricSize;
                        TunnelCutLenght = 22;
                        TunnelWt = ((TunnelCutLenght * (Utility.SafeConvertToDouble(comboTunnelGSM.Text) + Utility.SafeConvertToDouble(comboTunnelLamiGSM.Text)) * BodyFabricSize * 2) / 10000000);
                        TunnelTotalMtr = ((TunnelCutLenght * _BagQty * 2) / 100);
                               //+ .1 * ((TunnelCutLenght * _BagQty * 2) / 100);

                    }
                    else if (comboTunnelDesign.SelectedIndex == 1) //StoreSack name changes to plastene on 20th July 2017
                    {
                        if (_Type == 1) //External
                        {
                            BodyFabricSize = (_BagWidth + 10);
                            BodyCutLenght = ((_BagHeight * 2) + _BagLenght) + 142;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 18;
                                BodyCutLenght = ((_BagHeight * 2) + _BagLenght) + 142 + 7;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                        if (_Type == 0) //internal
                        {
                            BodyFabricSize = (_BagWidth + 15);
                            BodyCutLenght = (_BagHeight * 2) + _BagLenght + 152;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 18;
                                BodyCutLenght = (_BagHeight * 2) + _BagLenght + 152 + 18;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                        TunnelFabricSize = BodyFabricSize;
                        TunnelCutLenght = 28;
                        TunnelWt = ((TunnelCutLenght * (Utility.SafeConvertToDouble(comboTunnelGSM.Text) + Utility.SafeConvertToDouble(comboTunnelLamiGSM.Text)) * BodyFabricSize * 2) / 10000000);
                        TunnelTotalMtr = ((TunnelCutLenght * _BagQty * 2) / 100);
                             //  + .1 * ((TunnelCutLenght * _BagQty * 2) / 100);
                    }
                    else if (comboTunnelDesign.SelectedIndex == 3) //Greif Design name changes to plastene on 20th July 2017
                    {
                        if (_Type == 1) //External
                        {
                            // Body: Fabric Size-  width+20, Cut Size- (H*2+W)*2+42
                            //Side : Fabric Size- Length+10, Cut Size- (H+10)

                            BodyFabricSize = (_BagWidth + 16);
                            BodyCutLenght = (((_BagHeight * 2) + _BagLenght) * 2) + 42;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 18;
                                BodyCutLenght = (((_BagHeight * 2) + _BagLenght) * 2) + 42 + 18;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                        if (_Type == 0) //internal
                        {
                            BodyFabricSize = (_BagWidth + 20);
                            BodyCutLenght = (((_BagHeight * 2) + _BagLenght) * 2) + 42;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 27;//20+7 from 18 .23.08.2021
                                BodyCutLenght = (((_BagHeight * 2) + _BagLenght) * 2) + 42 + 18;
                            }
                            BodyWt = BodyCutLenght * Utility.SafeConvertToDouble(_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }

                        TunnelFabricSize = BodyFabricSize;
                        TunnelCutLenght = 22;
                        TunnelWt = ((TunnelCutLenght * (Utility.SafeConvertToDouble(comboTunnelGSM.Text) + Utility.SafeConvertToDouble(comboTunnelLamiGSM.Text)) * BodyFabricSize * 2) / 10000000);
                        TunnelTotalMtr = ((TunnelCutLenght * _BagQty * 2) / 100);
                              // + .1 * ((TunnelCutLenght * _BagQty * 2) / 100);
                    }
                    //else //Wickes remove by dilen on 20th July 2017
                    //{
                    //    if (_Type == 0) //Internal
                    //    {
                    //        BodyFabricSize = _BagWidth + 12;
                    //        BodyCutLenght = (_BagHeight * 2) + _BagLenght + 14;
                    //        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;

                    //        TunnelFabricSize = _BagWidth + 5;
                    //        TunnelCutLenght = 140;
                    //        TunnelWt = ((TunnelCutLenght * (Utility.SafeConvertToDouble(comboTunnelGSM.Text) + Utility.SafeConvertToDouble(comboTunnelLamiGSM.Text)) * TunnelFabricSize * 2) / 10000000);
                    //        TunnelTotalMtr = ((TunnelCutLenght * _BagQty * 2) / 100)
                    //           + .1 * ((TunnelCutLenght * _BagQty * 2) / 100);
                    //    }

                    //    else if (_Type == 1) //External
                    //    {
                    //        BodyFabricSize = _BagWidth + 8;
                    //        BodyCutLenght = (_BagHeight * 2) + _BagLenght + 6;
                    //        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                    //        TunnelFabricSize = _BagWidth;
                    //        TunnelCutLenght = 140;
                    //        TunnelWt = ((TunnelCutLenght * (Utility.SafeConvertToDouble(comboTunnelGSM.Text) + Utility.SafeConvertToDouble(comboTunnelLamiGSM.Text)) * TunnelFabricSize * 2) / 10000000);
                    //        TunnelTotalMtr = ((TunnelCutLenght * _BagQty * 2) / 100)
                    //           + .1 * ((TunnelCutLenght * _BagQty * 2) / 100);
                    //    }
                    //}
                }
                else
                {

                    if (comboBody2.SelectedIndex == 3) //Wider Fold
                    {
                        if (_Type == 0) //Internal
                        {
                            BodyFabricSize = (_BagLenght + 13);
                            BodyCutLenght = ((_BagHeight * 2) + _BagWidth + 15); //changes on dated 18th July 2017 as per dilen
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 18;
                                BodyCutLenght = ((_BagHeight * 2) + _BagWidth + 18);
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                        else if (_Type == 1) //External
                        {
                            BodyFabricSize = (_BagLenght + 10);
                            BodyCutLenght = (_BagHeight * 2) + _BagWidth + 8;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 18;
                                BodyCutLenght = (_BagHeight * 2) + _BagWidth + 15;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                    }
                    else if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated & sulzer 
                    {
                        if (_Type == 0) //Internal
                        {
                            BodyFabricSize = (_BagLenght + 4); //BodyFabricSize = (_BagWidth + 5); change by manish on 20th July 2022

                            BodyCutLenght = ((_BagHeight * 2) + _BagWidth + 15); // shard by 09th aug
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagLenght + 12; // 12 from 18 as disc. to add 7 in double fold 23.08.2021
                                BodyCutLenght = ((_BagHeight * 2) + _BagWidth + 21);
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                        else if (_Type == 1) //External
                        {
                            BodyFabricSize = _BagLenght;
                            BodyCutLenght = (_BagHeight * 2) + _BagWidth + 7;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = (_BagLenght + 8);
                                BodyCutLenght = (_BagHeight * 2) + _BagWidth + 14;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                    }
                    else if (comboBody2.SelectedIndex == 8) //Ventilated & sulzer 
                    {
                        if (_Type == 0) //Internal
                        {
                            BodyFabricSize = (_BagWidth + 20);
                            BodyCutLenght = (((_BagHeight * 2) + _BagLenght) * 2) + 42; // shard by 09th aug
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 18;
                                BodyCutLenght = (((_BagHeight * 2) + _BagLenght) * 2) + 42 + 18;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                        else if (_Type == 1) //External
                        {
                            BodyFabricSize = (_BagWidth + 20);
                            BodyCutLenght = (((_BagHeight * 2) + _BagLenght) * 2) + 42;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = (_BagWidth + 27);
                                BodyCutLenght = (((_BagHeight * 2) + _BagLenght) * 2) + 42 + 18;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                    }


                    else if (comboBody3.SelectedIndex == 1 || comboBody3.SelectedIndex == 3) //UN with FS/DS UN+FDA 11.08.2021
                    {
                        if (_Type == 0) //Internal
                        {
                            BodyFabricSize = (_BagLenght + 15);
                            BodyCutLenght = ((_BagHeight * 2) + _BagWidth + 19);
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 18;
                                BodyCutLenght = ((_BagHeight * 2) + _BagWidth + 18);
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                        else if (_Type == 1) //External
                        {
                            BodyFabricSize = (_BagLenght + 10);
                            BodyCutLenght = (_BagHeight * 2) + _BagWidth + 8;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 18;
                                BodyCutLenght = (_BagHeight * 2) + _BagWidth + 15;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                    }
                    else if (comboBody3.SelectedIndex == 0 || comboBody3.SelectedIndex == 2)  //Std
                    {// comboBody3.SelectedIndex == 2  is added by Rikin on 10-feb-2015 after discussion with Dilen ji 
                        if (_Type == 0) //Internal
                        {
                            BodyFabricSize = (_BagLenght + 11); //changes on dated 18th July 2017 as per dilen
                            BodyCutLenght = ((_BagHeight * 2) + _BagWidth + 15); //changes on dated 18th July 2017 as per dilen
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagLenght + 18;
                                BodyCutLenght = ((_BagHeight * 2) + _BagWidth + 23); //23.08.2021 change to 23 from 18
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                        else if (_Type == 1) //External
                        {

                            BodyFabricSize = (_BagLenght + 7);//changes on dated 18th July 2017 as per dilen
                            BodyCutLenght = (_BagHeight * 2) + _BagWidth + 6;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 14;//23.08.2021 change 18 to 14 for outer
                                BodyCutLenght = (_BagHeight * 2) + _BagWidth + 13; //23.08.2021 change 18 to 15 for outer
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                        }
                    }
                    if (comboLoopConst.SelectedIndex == 3) //Full Loop + Cross Corner
                    {
                        if (_Type == 0) //Internal
                        {
                            BodyFabricSize = (_BagLenght + 11); //changes on dated 18th July 2017 as per dilen
                            BodyCutLenght = ((_BagHeight * 2) + _BagWidth + 14);
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagLenght + 18;
                                BodyCutLenght = ((_BagHeight * 2) + _BagWidth + 18);
                            }
                            BodyWt = (BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize);
                            //  + .1 * (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * BodyFabricSize);
                        }
                        else if (_Type == 1) //External
                        {

                            BodyFabricSize = (_BagLenght + 7); //changes on dated 18th July 2017 as per dilen
                            BodyCutLenght = (_BagHeight * 2) + _BagWidth + 6;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagLenght + 18;
                                BodyCutLenght = BodyCutLenght + 18;
                            }
                            BodyWt = (BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize);
                            //   + .1 * (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * BodyFabricSize);
                        }
                    }

                }

            }


            if (_BodyIndex1 == 1 || _BodyIndex1 == 13) // Circular
            {
                if (_Type == 0) //Internal
                {

                    BodyFabricSize = _BagLenght + _BagWidth;
                    BodyCutLenght = (_BagHeight + 11); //changes on dated 18th July 2017 as per dilen
                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                    {
                        BodyFabricSize = _BagLenght + _BagWidth ;
                        BodyCutLenght = (_BagHeight + 18);
                    }
                    BodyWt = (BodyCutLenght * BodyFabricSize * 2 * (_BagGSM + _BagLamiGSM));
                    // + .1111 * (BodyCutLenght * BodyFabricSize * 2 * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)));
                }
                else
                {
                    BodyFabricSize = _BagLenght + _BagWidth;
                    BodyCutLenght = (_BagHeight + 7);//changes on dated 18th July 2017 as per dilen
                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                    {
                        BodyFabricSize = _BagLenght + _BagWidth ;
                        BodyCutLenght = (_BagHeight + 14);
                    }
                    BodyWt = (BodyCutLenght * BodyFabricSize * 2 * (_BagGSM + _BagLamiGSM));
                    //    + .1111 * (BodyCutLenght * BodyFabricSize * 2 * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)));

                }
            }

            if (_BodyIndex1 == 3) // 4 Panel
            {
                //  By Rikin on 12-mar-2015 as per ajay. for ventilated bag
                if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                {
                    if (_Type == 0) //Internal
                    {
                        BodyFabricSize = (_BagLenght + 4);
                        BodyCutLenght = (_BagHeight + 11); //changes on dated 18th July 2017 as per dilen
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagLenght + 18;
                            BodyCutLenght = _BagHeight + 18;
                        }
                        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize * 2;
                    }
                    else if (_Type == 1) //External
                    {
                        BodyFabricSize = _BagLenght; // change by manish  _BagLenght
                       // BodyCutLenght = (_BagHeight * 2) + _BagLenght + 7; //change by manish on 22nd July
                        BodyCutLenght = _BagHeight + 7;
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = (_BagWidth + 7);
                            BodyCutLenght = (_BagHeight * 2) + _BagLenght + 14;
                        }
                        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize * 2;
                    }
                }

                else
                //{
                //    if (textBodyL.Text == textBodyW.Text)
                //    {
                //        //if (_Type  == 0) //Internal
                //        //{
                //        //    BodyFabricSize = _BagLenght + 12;
                //        //    BodyCutLenght = _BagHeight + 12;
                //        //    BodyWt = BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * 4 * BodyFabricSize;
                //        //}
                //        //else if (_Type  == 1) //External
                //        //{
                //        //    BodyFabricSize = (_BagLenght + 8);
                //        //    BodyCutLenght = (_BagHeight + 8);
                //        //    BodyWt = BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * 4 * BodyFabricSize;
                //        //}
                //    }
                //    else
                {
                    if (_Type == 0) //Internal
                    {
                        BodyFabricSize = _BagLenght + 11; //changes on dated 18th July 2017 as per dilen
                        BodyCutLenght = _BagHeight + 11;
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagLenght + 18;
                            BodyCutLenght = _BagHeight + 18;
                        }
                        if (_BagLenght == _BagWidth)
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize; //14.10.2021 change  
                        else
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize; //14.10.2021 change 4 to 2
                    }
                    else if (_Type == 1) //External
                    {
                        BodyFabricSize = (_BagLenght + 7); //changes on dated 18th July 2017 as per dilen
                        BodyCutLenght = (_BagHeight + 7);
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 14;//change to 14 from 18
                            BodyCutLenght = _BagHeight + 14;
                        }
                        if (_BagLenght == _BagWidth)
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize; //14.10.2021  
                        else
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize; //14.10.2021 change 4 to 2
                        //+ (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * 2 * (_BagWidth + 8));
                    }
                }
                //if (comboLoopConst.SelectedIndex == 3) //Full Loop + Cross Corner
                //{
                //    if (_Type  == 0) //Internal
                //    {
                //        BodyFabricSize = _BagLenght + 12;
                //        BodyCutLenght = _BagHeight + 12;
                //        BodyWt = (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * 4 * BodyFabricSize)
                //                  + .1111 * (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * 4 * BodyFabricSize);
                //    }
                //    else if (_Type  == 1) //External
                //    {
                //        BodyFabricSize = (_BagLenght + 8);
                //        BodyCutLenght = (_BagHeight + 8);
                //        BodyWt = (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * 4 * BodyFabricSize)
                //         + .1111 * (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * 4 * BodyFabricSize);
                //    }
                //}
                // }
            }

            if (_BodyIndex1 == 2) //Buffle
            {
                if (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 6) //4 Side Buffle,Middle Seam
                {
                    if (textBodyW.Text == textBodyL.Text)
                    {
                        if (_Type == 0) //Internal
                        {
                            if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                            {
                                BodyFabricSize = _BagLenght + 4; //changes on dated 18th July 2017 as per dilen
                                BodyCutLenght = _BagHeight + 11;//changes on dated 18th July 2017 as per dilen
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 18;
                                    BodyCutLenght = _BagHeight + 18;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                            }
                            else if (comboBody2.SelectedIndex == 12) //Almatis Folder 19.06.2021
                            {
                                BodyFabricSize = _BagLenght + 15;
                                BodyCutLenght = _BagHeight + 12;
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 18;
                                    BodyCutLenght = _BagHeight + 18;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                            }
                            else if (comboBody2.SelectedIndex == 13) //Almatis [Std Fold] 19.06.2021
                            {
                                BodyFabricSize = _BagLenght + 12;
                                BodyCutLenght = _BagHeight + 12;
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 18;
                                    BodyCutLenght = _BagHeight + 18;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                            }
                            else
                            {
                                if (comboBody2.SelectedIndex == 3) //Wider Fold
                                {

                                    BodyFabricSize = _BagLenght + 14; //changes on dated 18th July 2017 as per dilen
                                    BodyCutLenght = _BagHeight + 14;//changes on dated 18th July 2017 as per dilen
                                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                    {
                                        BodyFabricSize = _BagWidth + 18;
                                        BodyCutLenght = _BagHeight + 18;
                                    }
                                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                                }
                                else
                                {
                                    BodyFabricSize = _BagLenght + 11; //changes on dated 18th July 2017 as per dilen
                                    BodyCutLenght = _BagHeight + 11;//changes on dated 18th July 2017 as per dilen
                                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                    {
                                        BodyFabricSize = _BagWidth + 18;
                                        BodyCutLenght = _BagHeight + 18;
                                    }
                                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;

                                }
                            }
                        }
                        else if (_Type == 1) //External
                        {
                            if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                            {
                                BodyFabricSize = _BagLenght;//changes on dated 18th July 2017 as per dilen
                                BodyCutLenght = (_BagHeight + 7);//changes on dated 18th July 2017 as per dilen
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 8;
                                    BodyCutLenght = _BagHeight + 14;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                            }
                            else if (comboBody2.SelectedIndex == 12) //Almatis Folder 19.06.2021
                            {
                                BodyFabricSize = _BagLenght + 11;
                                BodyCutLenght = _BagHeight + 8;
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 18;
                                    BodyCutLenght = _BagHeight + 15;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                            }
                            else if (comboBody2.SelectedIndex == 13) //Almatis [Std Fold] 19.06.2021
                            {
                                BodyFabricSize = _BagLenght + 8;
                                BodyCutLenght = _BagHeight + 8;
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 15;
                                    BodyCutLenght = _BagHeight + 15;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                            }
                            else
                            {
                                if (comboBody2.SelectedIndex == 3) //Wider Fold
                                {

                                    BodyFabricSize = _BagLenght + 8; //changes on dated 18th July 2017 as per dilen
                                    BodyCutLenght = _BagHeight + 8;//changes on dated 18th July 2017 as per dilen
                                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                    {
                                        BodyFabricSize = _BagWidth + 15;
                                        BodyCutLenght = _BagHeight + 15;
                                    }
                                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                                }
                                else
                                {
                                    BodyFabricSize = (_BagLenght + 7);//changes on dated 18th July 2017 as per dilen
                                    BodyCutLenght = (_BagHeight + 7);//changes on dated 18th July 2017 as per dilen
                                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                    {
                                        BodyFabricSize = _BagWidth + 14;
                                        BodyCutLenght = _BagHeight + 14;
                                    }
                                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_Type == 0) //Internal
                        {
                            if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                            {
                                BodyFabricSize = _BagLenght + 4;//changes on dated 18th July 2017 as per dilen
                                BodyCutLenght = _BagHeight + 11;//changes on dated 18th July 2017 as per dilen
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 18;
                                    BodyCutLenght = _BagHeight + 18;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                            }
                            else if (comboBody2.SelectedIndex == 12) //Almatis Folder 19.06.2021
                            {
                                BodyFabricSize = _BagLenght + 15;
                                BodyCutLenght = _BagHeight + 12;
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 18;
                                    BodyCutLenght = _BagHeight + 18;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                            }
                            else if (comboBody2.SelectedIndex == 13) //Almatis [Std Fold] 19.06.2021
                            {
                                BodyFabricSize = _BagLenght + 12;
                                BodyCutLenght = _BagHeight + 12;
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 18;
                                    BodyCutLenght = _BagHeight + 18;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                            }
                            else
                            {
                                if (comboBody2.SelectedIndex == 3) //Wider Fold
                                {

                                    BodyFabricSize = _BagLenght + 14; //changes on dated 18th July 2017 as per dilen
                                    BodyCutLenght = _BagHeight + 14;//changes on dated 18th July 2017 as per dilen
                                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                    {
                                        BodyFabricSize = _BagWidth + 18;
                                        BodyCutLenght = _BagHeight + 18;
                                    }
                                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                                }
                                else
                                {
                                    BodyFabricSize = _BagLenght + 11;//changes on dated 18th July 2017 as per dilen
                                    BodyCutLenght = _BagHeight + 11;//changes on dated 18th July 2017 as per dilen
                                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                    {
                                        BodyFabricSize = _BagWidth + 18;
                                        BodyCutLenght = _BagHeight + 18;
                                    }
                                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                                }
                            }

                        }
                        else if (_Type == 1) //External
                        {
                            if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                            {
                                BodyFabricSize = _BagLenght;//changes on dated 18th July 2017 as per dilen
                                BodyCutLenght = (_BagHeight + 7);//changes on dated 18th July 2017 as per dilen
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 8;
                                    BodyCutLenght = _BagHeight + 14;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                            }
                            else if (comboBody2.SelectedIndex == 12) //Almatis Folder 19.06.2021
                            {
                                BodyFabricSize = _BagLenght + 11;
                                BodyCutLenght = _BagHeight + 8;
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 18;
                                    BodyCutLenght = _BagHeight + 15;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                            }
                            else if (comboBody2.SelectedIndex == 13) //Almatis [Std Fold] 19.06.2021
                            {
                                BodyFabricSize = _BagLenght + 8;
                                BodyCutLenght = _BagHeight + 8;
                                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                {
                                    BodyFabricSize = _BagWidth + 15;
                                    BodyCutLenght = _BagHeight + 15;
                                }
                                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                            }
                            else
                            {
                                if (comboBody2.SelectedIndex == 3) //Wider Fold
                                {

                                    BodyFabricSize = (_BagLenght + 8);//changes on dated 18th July 2017 as per dilen
                                    BodyCutLenght = (_BagHeight + 8);//changes on dated 18th July 2017 as per dilen
                                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                    {
                                        BodyFabricSize = _BagWidth + 15;
                                        BodyCutLenght = _BagHeight + 15;
                                    }
                                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                                }
                                else
                                {

                                    BodyFabricSize = (_BagLenght + 7);//changes on dated 18th July 2017 as per dilen
                                    BodyCutLenght = (_BagHeight + 7);//changes on dated 18th July 2017 as per dilen
                                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                                    {
                                        BodyFabricSize = _BagWidth + 14;
                                        BodyCutLenght = _BagHeight + 14;
                                    }
                                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                                }
                            }
                        }
                    }
                }
                else if (comboBuffleType.SelectedIndex == 1)
                {
                    if (textBodyW.Text == textBodyL.Text)
                    {
                        if (_Type == 0) //Internal
                        {
                            BodyFabricSize = _BagLenght + 12;
                            BodyCutLenght = _BagHeight + 12;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 18;
                                BodyCutLenght = BodyCutLenght + 18;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;

                        }
                        else if (_Type == 1) //External
                        {
                            BodyFabricSize = (_BagLenght + 8);
                            BodyCutLenght = (_BagHeight + 8);
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 15;
                                BodyCutLenght = _BagHeight + 15;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                        }
                    }
                    else
                    {
                        if (_Type == 0) //Internal
                        {
                            BodyFabricSize = _BagLenght + 12;
                            BodyCutLenght = _BagHeight + 12;
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 18;
                                BodyCutLenght = _BagHeight + 18;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                        }
                        else if (_Type == 1) //External
                        {
                            BodyFabricSize = (_BagLenght + 8);
                            BodyCutLenght = (_BagHeight + 8);
                            if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                            {
                                BodyFabricSize = _BagWidth + 15;
                                BodyCutLenght = _BagHeight + 15;
                            }
                            BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                        }
                    }
                    // BodyWt = BodyWt + ((11.11 / 100) * BodyWt);
                }
                else if (comboBuffleType.SelectedIndex == 2 || comboBuffleType.SelectedIndex == 3) //Tube + Corner + Buffle
                {
                    if (_Type == 0) //Internal
                    {
                        BodyFabricSize = _BagLenght + _BagWidth + 8;
                        BodyCutLenght = (_BagHeight + 12);
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 18;
                            BodyCutLenght = _BagHeight + 18;
                        }
                        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                    }
                    else
                    {
                        BodyFabricSize = _BagLenght + _BagWidth;
                        BodyCutLenght = (2 * BodyFabricSize) * (_BagHeight + 8);
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 7;
                            BodyCutLenght = (2 * BodyFabricSize) * (_BagHeight + 15);
                        }
                        BodyWt = (BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize);
                    }
                }
                else if (comboBuffleType.SelectedIndex == 4) // 2Panel Corner + Buffle
                {
                    if (_Type == 0) //Internal
                    {
                        BodyFabricSize = _BagLenght + _BagWidth + 20;
                        BodyCutLenght = (_BagHeight + 12);
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 18;
                            BodyCutLenght = _BagHeight + 18;
                        }
                        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize;
                    }
                    else //External
                    {
                        BodyFabricSize = _BagLenght + _BagWidth + 16;
                        BodyCutLenght = (_BagHeight + 8);
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 23;
                            BodyCutLenght = _BagHeight + 15;
                        }
                        BodyWt = (BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize);
                    }
                }
                else if (comboBuffleType.SelectedIndex == 5) // 2Panel + Cross Corner + Buffle
                {
                    if (_Type == 0) //Internal
                    {
                        BodyFabricSize = _BagLenght + _BagWidth + 12;
                        BodyCutLenght = (_BagHeight + 12);
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 18;
                            BodyCutLenght = _BagHeight + 18;
                        }
                        BodyWt = (BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize);
                        //  + .1111 * (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * 2 * BodyFabricSize);
                    }
                    else //External
                    {
                        BodyFabricSize = _BagLenght + _BagWidth + 8;
                        BodyCutLenght = (_BagHeight + 8);
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 15;
                            BodyCutLenght = _BagHeight + 15;
                        }
                        BodyWt = (BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize);
                        //   + .1111 * (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * 2 * BodyFabricSize);
                    }

                }
            }


            if (_BodyIndex1 == 4) // Tube + Corner
            {
                if (_Type == 0) //Internal
                {
                    BodyFabricSize = (_BagLenght + 4 + _BagWidth + 4);
                    BodyCutLenght = (_BagHeight + 12);
                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                    {
                        BodyFabricSize = _BagWidth + 18;
                        BodyCutLenght = _BagHeight + 18;
                    }
                    BodyWt = (BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize);
                    //     + .1 * (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)) * 2 * BodyFabricSize);
                }
                else if (_Type == 1) //External
                {
                    BodyFabricSize = (_BagLenght + 4 + _BagWidth + 4);
                    BodyCutLenght = (_BagHeight + 8);
                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                    {
                        BodyFabricSize = _BagWidth + 15;
                        BodyCutLenght = _BagHeight + 15;
                    }
                    BodyWt = (BodyCutLenght * (_BagGSM + _BagLamiGSM) * 2 * BodyFabricSize);
                    //               + .1 * (BodyCutLenght * (Utility.SafeConvertToDouble (comboBodyGSM.Text) + Utility.SafeConvertToDouble (comboBodyLamiGSM.Text)));
                }
            }

            if (_BodyIndex1 == 5 || _BodyIndex1 == 7) // Single Loop ,SingleLoop+ 4 Side
            {
                double TotalBodyHt = 0;
                if (textSlitHt.Text == "")
                    SlitHt = (Math.Sqrt(((_BagLenght) * (_BagLenght)) +
                               (_BagWidth) * (_BagWidth))) / 2;
                else
                    SlitHt = Utility.SafeConvertToDouble(textSlitHt.Text);

                SlitHt = Math.Round(SlitHt, 2);
                textSlitHt.Text = SlitHt.ToString();

                if (_Type == 0) //internal
                    TotalBodyHt = SlitHt + Utility.SafeConvertToDouble(textFillHt.Text) + Utility.SafeConvertToDouble(textStartSewnBaseHt.Text) + 12;
                else //External
                    TotalBodyHt = SlitHt + Utility.SafeConvertToDouble(textFillHt.Text) + 8;

                if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                    TotalBodyHt = TotalBodyHt + 10;
                else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                    TotalBodyHt = TotalBodyHt + 15;
                else
                    TotalBodyHt = TotalBodyHt + 20;

                //Removed by Rikin on 27-O5-2015 as below code is no longer in use. added a new option of star base HT beside of Fill nd slit ht.
                //if (comboBoxbottomsubtype.SelectedIndex == 4) //StarBased
                //{
                //    if (_BagLenght > _BagWidth)
                //        TotalBodyHt += (_BagLenght) / 2;
                //    else
                //        TotalBodyHt += (_BagWidth) / 2;
                //}
                BodyFabricSize = (_BagLenght + _BagWidth);
                BodyCutLenght = TotalBodyHt;
                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                {
                    if(_BodyIndex1 == 5)
                        BodyFabricSize = (_BagWidth + _BagLenght);
                    else
                        BodyFabricSize = (_BagWidth) + 18;
                 
                    BodyCutLenght = TotalBodyHt + 18;
                }
                BodyWt = 2 * BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;

                TotalHt = TotalBodyHt;
            }


            if (_BodyIndex1 == 6 || _BodyIndex1 == 8) // Double Loop 
            {
                double TotalBodyHt = 0;
                //Changed By Rikin on 10-Feb-2015 as need to value of text rather than below.
                //if (_BagLenght > _BagWidth)
                //    SlitHt = (_BagLenght) / 2;
                //else
                //    SlitHt = (_BagWidth) / 2;
                SlitHt = Utility.SafeConvertToDouble(textSlitHt.Text);

                if (_Type == 0) //Internal
                    TotalBodyHt = SlitHt + Utility.SafeConvertToDouble(textFillHt.Text) + 12;
                else
                    TotalBodyHt = SlitHt + Utility.SafeConvertToDouble(textFillHt.Text) + 8;

                if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                    TotalBodyHt = TotalBodyHt + 10;
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) > 1000 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250)
                    TotalBodyHt = TotalBodyHt + 15;
                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) > 1000 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                    TotalBodyHt = TotalBodyHt + 15;
                else
                    TotalBodyHt = TotalBodyHt + 20;

                if (comboBoxbottomsubtype.SelectedIndex == 4) //StarBased
                {
                    if (_BagLenght > _BagWidth)
                        TotalBodyHt += (_BagLenght) / 2;
                    else
                        TotalBodyHt += (_BagWidth) / 2;
                }

                BodyFabricSize = (_BagLenght + _BagWidth);
                BodyCutLenght = TotalBodyHt;
                if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                {
                    if(_BodyIndex1 == 6)
                        BodyFabricSize = _BagWidth + _BagLenght;
                    else 
                        BodyFabricSize = _BagWidth + 18;
                    BodyCutLenght = TotalBodyHt + 18;
                }

                BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize * 2;

                TotalHt = TotalBodyHt;
            }

            if (_BodyIndex1 == 9)  // Conical Bag Three Piece
            {

                if (_Type == 0) //Internal
                {
                    BodyFabricSize = _BagLenght + 12;
                    BodyCutLenght = _BagHeight + 12;
                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                    {
                        BodyFabricSize = _BagWidth + 18;
                        BodyCutLenght = _BagHeight + 18;
                    }
                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                }
                else if (_Type == 1) //External
                {
                    BodyFabricSize = (_BagLenght + 8);
                    BodyCutLenght = (_BagHeight + 8);
                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                    {
                        BodyFabricSize = _BagWidth + 15;
                        BodyCutLenght = _BagHeight + 15;
                    }
                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * 4 * BodyFabricSize;
                }
            }

            if (_BodyIndex1 == 10)  // Conical Bag Single Piece
            {
                double OneSideDia = (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) * 3.14) / 4;
                if (_Type == 0) //internal
                {
                    BodyFabricSize = (_BagLenght + 12);
                    BodyCutLenght = ((_BagWidth - OneSideDia) / 2) + 14 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + _BagHeight;
                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                    {
                        BodyFabricSize = _BagWidth + 18;
                        BodyCutLenght = ((_BagWidth - OneSideDia) / 2) + 14 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + _BagHeight + 18;
                    }
                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize * 4;
                }
                else
                {
                    BodyFabricSize = (_BagLenght + 8);
                    BodyCutLenght = ((_BagWidth - OneSideDia) / 2) + 10 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + _BagHeight;
                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                    {
                        BodyFabricSize = _BagWidth + 15;
                        BodyCutLenght = ((_BagWidth - OneSideDia) / 2) + 10 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + _BagHeight + 17;
                    }
                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize * 4;
                }
            }

            if (_BodyIndex1 == 11)  // Hood Bag/Covered Bag
            {

                if (comboBody2.SelectedIndex == 10)
                {
                    if (_Type == 0) //internal
                    {
                        BodyFabricSize = (_BagLenght + 12);
                        BodyCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(_BagWidth) + 15;
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 18;
                            BodyCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(_BagWidth) + 18;
                        }
                        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                    }
                    else
                    {
                        BodyFabricSize = (_BagLenght + 8);
                        BodyCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(_BagWidth) + 8;
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 15;
                            BodyCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(_BagWidth) + 15;
                        }
                        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                    }
                }
                else if (comboBody2.SelectedIndex == 11)
                {
                    if (_Type == 0) //internal
                    {
                        BodyFabricSize = (_BagLenght + 12);
                        BodyCutLenght = _BagHeight + 12;
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 18;
                            BodyCutLenght = _BagHeight + 18;
                        }
                        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize * 2;
                    }
                    else
                    {
                        BodyFabricSize = (_BagLenght + 8);
                        BodyCutLenght = _BagHeight + 8;
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 15;
                            BodyCutLenght = _BagHeight + 15;
                        }
                        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize * 2;
                    }
                }

                else
                {
                    if (_Type == 0) //internal
                    {
                        BodyFabricSize = (_BagLenght + 12);
                        BodyCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(comboHoodSIze.Text) + 14;
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 18;
                            BodyCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(comboHoodSIze.Text) + 18;
                        }
                        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                    }
                    else
                    {
                        BodyFabricSize = (_BagLenght + 8);
                        BodyCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(comboHoodSIze.Text) + 8;
                        if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                        {
                            BodyFabricSize = _BagWidth + 15;
                            BodyCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(comboHoodSIze.Text) + 15;
                        }
                        BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                    }
                }
            }

            if (_BodyIndex1 == 12) // Double Layer Tunnel Lift Bag
            {
                if (_Type == 0) //internal
                {
                    BodyFabricSize = (_BagWidth + 8);
                    BodyCutLenght = (_BagHeight * 2) + _BagLenght + 160;
                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                    {
                        BodyFabricSize = _BagWidth + 18;
                        BodyCutLenght = (_BagHeight * 2) + _BagLenght + 160 + 18;
                    }
                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                }
                else
                {
                    BodyFabricSize = (_BagWidth + 8);
                    BodyCutLenght = (_BagHeight * 2) + _BagLenght + 150;
                    if (checkBoxDoubleFoldBody.Checked) //18.08.2021 add Double Fold
                    {
                        BodyFabricSize = _BagWidth + 15;
                        BodyCutLenght = (_BagHeight * 2) + _BagLenght + 157;
                    }
                    BodyWt = BodyCutLenght * (_BagGSM + _BagLamiGSM) * BodyFabricSize;
                }
            }

            //if (comboBoxbottomsubtype.SelectedIndex == 4 && (_BodyIndex1 == 5 || _BodyIndex1 == 6)) //Star Based
            //{
            //    if (_Type  == 0) //internal
            //    {
            //        double slitHt = (Math.Sqrt(((_BagLenght) * (_BagLenght)) +
            //                        (_BagWidth) * (_BagWidth))) / 2;
            //        BodyCutLenght += slitHt + _BagHeight + 12 + (_BagWidth / 2);
            //    }
            //    else //External
            //    {
            //        double slitHt = (Math.Sqrt(((_BagLenght) * (_BagLenght)) +
            //                       (_BagWidth) * (_BagWidth))) / 2;
            //        BodyCutLenght += slitHt + _BagHeight + 8 + (_BagWidth / 2);
            //    }
            //}
            if (checkBoxRF.Checked)
                BodyWt += BodyWt * 0.1111;

            if (textBodyNo.Text != "")
                BodyWt = BodyWt * Convert.ToInt32(textBodyNo.Text);
            return BodyWt;
        }
        private double SideWtFormula()
        {

            if (_BodyIndex1 == 0) //Upanel
            {
                if (comboBody2.SelectedIndex == 3) //Wider Fold
                {
                    if (_Type == 0) //Internal
                    {
                        SideFabricSize = (_BagWidth + 13);
                        SideCutLenght = _BagHeight + 11; //changes on dated 18th July 2017 as per dilen
                        //   SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 2;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 18;
                            SideCutLenght = _BagHeight + 18;
                        }
                    }
                    else if (_Type == 1) //External
                    {
                        SideFabricSize = (_BagWidth + 10);
                        SideCutLenght = _BagHeight + 7;//changes on dated 18th July 2017 as per dilen
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 17;
                            SideCutLenght = _BagHeight + 15;
                        }
                    }

                    SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 2;
                }

                else if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated 
                {
                    if (_Type == 0) //Internal
                    {
                        SideFabricSize = (_BagWidth + 4);// Changed from textBodyw to textBodyL by Rikin on 10-Feb-2015
                        SideCutLenght = (_BagHeight + 12);
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagLenght + 18;
                            SideCutLenght = BodyCutLenght + 18;
                        }
                        //  SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 2;
                    }
                    else if (_Type == 1) //External
                    {
                        SideFabricSize = _BagWidth;
                        SideCutLenght = _BagHeight + 8;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagLenght + 8;
                            SideCutLenght = BodyCutLenght + 15;
                        }
                    }

                    SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 2;
                }
                else if (comboBody2.SelectedIndex == 8) //Ventilated 
                {
                    if (_Type == 0) //Internal
                    {
                        SideFabricSize = (_BagLenght + 10);// Changed from textBodyw to textBodyL by Rikin on 10-Feb-2015
                        SideCutLenght = (_BagHeight + 10);
                        //  SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 2;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagLenght + 18;
                            SideCutLenght = _BagHeight + 18;
                        }
                    }
                    else if (_Type == 1) //External
                    {
                        SideFabricSize = (_BagLenght + 10);
                        SideCutLenght = _BagHeight + 10;
                    }

                    SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 2;
                }

                else if (comboBody2.SelectedIndex == 9) //Sleeve bag
                {
                    if (_Type == 0) //Internal
                    {
                        SideFabricSize = (_BagLenght + 10);// Changed from textBodyw to textBodyL by Rikin on 10-Feb-2015
                        SideCutLenght = (BodyCutLenght + 18) + (Utility.SafeConvertToDouble(comboLoopL.Text) * 2);
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagLenght + 18;
                            SideCutLenght = (BodyCutLenght + 18) + (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 18;
                        }
                        //  SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 2;
                    }
                    else if (_Type == 1) //External
                    {
                        SideFabricSize = (_BagLenght + 10);
                        SideCutLenght = (_BagHeight + 14) + (Utility.SafeConvertToDouble(comboLoopL.Text) * 2);
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagLenght + 18;
                            SideCutLenght = (_BagHeight + 14) + (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 18;
                        }
                    }

                    SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 2;
                }
                else if (comboBody3.SelectedIndex == 1 || comboBody3.SelectedIndex == 3) //UN with FS/DS UN+FDA 11.08.2021
                {
                    if (_Type == 0) //Internal
                    {
                        SideFabricSize = _BagWidth + 15;
                        SideCutLenght = _BagHeight + 14;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 18;
                            SideCutLenght = _BagHeight + 18;
                        }
                        //  SideWt = SideFabricSize * SideCutLenght * 2 * (_BagSideGSM + _BagSideLamiGSM);
                    }
                    else if (_Type == 1) //External
                    {
                        SideFabricSize = _BagWidth + 8;
                        SideCutLenght = _BagHeight + 8;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 15;
                            SideCutLenght = _BagHeight + 15;
                        }
                    }

                    SideWt = SideFabricSize * SideCutLenght * 2 * (_BagSideGSM + _BagSideLamiGSM);
                }
                else
                {
                    if (checkBoxTunnel.Checked)
                    {
                        if (comboTunnelDesign.SelectedIndex == 3) // Grief design
                        {
                            // Side : Fabric Size- Length+10, Cut Size- (H+10)
                            SideFabricSize = _BagLenght + 10; //changes on dated 20th July 2017 as per dilen
                            SideCutLenght = _BagHeight + 10;//changes on dated 20th July 2017 as per dilen
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagLenght + 18;
                                SideCutLenght = _BagHeight + 18;
                            }
                        }
                        else
                        {
                            if (_Type == 0) //Internal
                            {
                                SideFabricSize = _BagLenght + 11; //changes on dated 18th July 2017 as per dilen
                                SideCutLenght = _BagHeight + 11;//changes on dated 18th July 2017 as per dilen
                                //   SideWt = SideFabricSize * SideCutLenght * 2 * (_BagSideGSM + _BagSideLamiGSM);
                                if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                {
                                    SideFabricSize = _BagLenght + 18;
                                    SideCutLenght = _BagHeight + 18;
                                }
                            }
                            else if (_Type == 1) //External
                            {
                                SideFabricSize = _BagLenght + 7; //changes on dated 18th July 2017 as per dilen
                                SideCutLenght = _BagHeight + 7;//changes on dated 18th July 2017 as per dilen
                                if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                {
                                    SideFabricSize = _BagLenght + 15;
                                    SideCutLenght = _BagHeight + 15;
                                }
                            }

                        }

                        SideWt = SideFabricSize * SideCutLenght * 2 * (_BagSideGSM + _BagSideLamiGSM);
                    }
                    else
                    {
                        if (_Type == 0) //Internal
                        {
                            SideFabricSize = _BagWidth + 11;   //changes on dated 18th July 2017 as per dilen
                            SideCutLenght = _BagHeight + 11;//changes on dated 18th July 2017 as per dilen
                            // SideWt = SideFabricSize * SideCutLenght * 2 * (_BagSideGSM + _BagSideLamiGSM);
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 18;
                                SideCutLenght = _BagHeight + 18;
                            }
                        }
                        else if (_Type == 1) //External
                        {
                            SideFabricSize = _BagWidth + 7;//changes on dated 18th July 2017 as per dilen
                            SideCutLenght = _BagHeight + 7;//changes on dated 18th July 2017 as per dilen
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 14;
                                SideCutLenght = _BagHeight + 14;
                            }
                        }
                        SideWt = SideFabricSize * SideCutLenght * 2 * (_BagSideGSM + _BagSideLamiGSM);
                    }
                }


                if (comboLoopConst.SelectedIndex == 3) //Full Loop + Cross Corner
                {
                    if (_Type == 0) //Internal
                    {
                        SideFabricSize = _BagWidth + 11; //changes on dated 18th July 2017 as per dilen
                        SideCutLenght = _BagHeight + 11;//changes on dated 18th July 2017 as per dilen
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 18;
                            SideCutLenght = _BagHeight + 18;
                        }
                        SideWt = (SideFabricSize * SideCutLenght * 2 * (_BagSideGSM + _BagSideLamiGSM));
                        //    + .1 * (SideFabricSize * SideCutLenght * 2 * (Utility.SafeConvertToDouble (comboSideGSM.Text) + Utility.SafeConvertToDouble (comboSideLamiGSM.Text)));
                    }
                    else if (_Type == 1) //External
                    {

                        SideFabricSize = _BagWidth + 7;//changes on dated 18th July 2017 as per dilen
                        SideCutLenght = _BagHeight + 7;//changes on dated 18th July 2017 as per dilen
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 15;
                            SideCutLenght = _BagHeight + 15;
                        }
                        SideWt = (SideFabricSize * SideCutLenght * 2 * (_BagSideGSM + _BagSideLamiGSM));
                        // + .1 * (SideFabricSize * SideCutLenght * 2 * (Utility.SafeConvertToDouble (comboSideGSM.Text) + Utility.SafeConvertToDouble (comboSideLamiGSM.Text)));
                    }
                }
            }

            if (_BodyIndex1 == 9) // Conical Bag
            {
                if (textBodyW.Text == textBodyL.Text)
                {
                    if (_Type == 0) //Internal
                    {
                        SideFabricSize = _BagWidth + 12;
                        SideCutLenght = _BagHeight + 12;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 18;
                            SideCutLenght = _BagHeight + 18;
                        }
                        SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght;
                    }
                    else if (_Type == 1) //External
                    {
                        SideFabricSize = _BagWidth + 8;
                        SideCutLenght = _BagHeight + 8;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 15;
                            SideCutLenght = _BagHeight + 15;
                        }
                        SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght;
                    }
                }
                else
                {
                    if (_Type == 0) //Internal
                    {
                        SideFabricSize = _BagWidth + 12;
                        SideCutLenght = _BagHeight + 12;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 18;
                            SideCutLenght = _BagHeight + 18;
                        }
                        SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght;
                    }
                    else if (_Type == 1) //External
                    {
                        SideFabricSize = _BagWidth + 8;
                        SideCutLenght = _BagHeight + 8;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 15;
                            SideCutLenght = _BagHeight + 15;
                        }
                        SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght;
                    }
                }
            }


            if (_BodyIndex1 == 3) // 4Panel 
            {
                if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated 
                {

                    if (textBodyW.Text == textBodyL.Text)
                    {
                        if (_Type == 0) //Internal
                        {
                            SideFabricSize = (_BagWidth + 4);
                            SideCutLenght = (_BagHeight + 11); //changes on dated 18th July 2017 as per dilen
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 18;
                                SideCutLenght = _BagHeight + 18;
                            }
                            SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 4;
                        }
                        else if (_Type == 1) //External
                        {
                            SideFabricSize = _BagWidth;
                            SideCutLenght = _BagHeight + 7;//changes on dated 18th July 2017 as per dilen
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 8;
                                SideCutLenght = _BagHeight + 14;
                            }
                            SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 4;
                        }
                    }
                    else
                    {
                        if (_Type == 0) //Internal
                        {
                            SideFabricSize = (_BagWidth + 4); //change by manish on 22nd july
                            SideCutLenght = (_BagHeight + 11);//changes on dated 18th July 2017 as per dilen
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 18;
                                SideCutLenght = _BagHeight + 18;
                            }
                            SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 2;
                        }
                        else if (_Type == 1) //External
                        {
                            SideFabricSize = _BagWidth;//change by manish on 22nd july
                            SideCutLenght = _BagHeight + 7;//changes on dated 18th July 2017 as per dilen
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 8;
                                SideCutLenght = _BagHeight + 15;
                            }
                            SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize * 2;
                        }
                    }
                }
                else if (checkBoxTunnel.Checked)
                {
                    if (comboTunnelDesign.SelectedIndex == 4)
                    {
                        if (_Type == 0) //Internal
                        {
                            SideFabricSize = _BagWidth + 15; //changes on dated 18th July 2017 as per dilen
                            SideCutLenght = _BagHeight + 11; //changes on dated 18th July 2017 as per dilen
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 18;
                                SideCutLenght = _BagHeight + 18;
                            }
                            SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght;
                        }
                        else if (_Type == 1) //External
                        {
                            SideFabricSize = _BagWidth + 11; //changes on dated 18th July 2017 as per dilen
                            SideCutLenght = _BagHeight + 7;//changes on dated 18th July 2017 as per dilen
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 18;
                                SideCutLenght = _BagHeight + 14;
                            }
                            SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght;
                        }
                        //Sides : Fabric Size- L+15, Cut Size- H+11
                        //Sides : Fabric Size- W+15, Cut Size- H+11
                    }
                }
                else
                {
                    if (comboConicalHeight.Text == "")
                        comboConicalHeight.Text = "0";

                    if (textBodyW.Text == textBodyL.Text)
                    {
                        if (_Type == 0) //Internal
                        {
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 18;
                                if (comboBoxbottomtype.SelectedIndex == 2)
                                {
                                    if (comboConicalHeight.Text == "0")
                                        SideCutLenght = _BagHeight + (Convert.ToInt32(_BagWidth / 2)) + 18;
                                    else
                                        SideCutLenght = _BagHeight + Convert.ToInt32(comboConicalHeight.Text) + 18; //changes on dated 18th July 2017 as per dilen
                                }
                                else
                                    SideCutLenght = _BagHeight + 18; //changes on dated 18th July 2017 as per dilen
                            }
                            else
                            {
                                SideFabricSize = _BagWidth + 11; //changes on dated 18th July 2017 as per dilen
                                if (comboBoxbottomtype.SelectedIndex == 2)
                                {
                                    if (comboConicalHeight.Text == "0")
                                        SideCutLenght = _BagHeight + (Convert.ToInt32(_BagWidth / 2)) + 11;
                                    else
                                        SideCutLenght = _BagHeight + Convert.ToInt32(comboConicalHeight.Text) + 11; //changes on dated 18th July 2017 as per dilen
                                }
                                else
                                    SideCutLenght = _BagHeight + 11; //changes on dated 18th July 2017 as per dilen

                            }
                            SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght;
                        }
                        else if (_Type == 1) //External
                        {
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 14;
                                if (comboBoxbottomtype.SelectedIndex == 2)
                                {
                                    if (comboConicalHeight.Text == "0")
                                        SideCutLenght = _BagHeight + (Convert.ToInt32(_BagWidth / 2)) + 14;
                                    else
                                        SideCutLenght = _BagHeight + Convert.ToInt32(comboConicalHeight.Text) + 14;
                                }
                                else
                                    SideCutLenght = _BagHeight + 14;
                            }
                            else
                            {
                                SideFabricSize = _BagWidth + 7; //changes on dated 18th July 2017 as per dilen
                                if (comboBoxbottomtype.SelectedIndex == 2)
                                {
                                    if (comboConicalHeight.Text == "0")
                                        SideCutLenght = _BagHeight + (Convert.ToInt32(_BagWidth / 2)) + 7;
                                    else
                                        SideCutLenght = _BagHeight + Convert.ToInt32(comboConicalHeight.Text) + 7; //changes on dated 18th July 2017 as per dilen
                                }
                                else
                                    SideCutLenght = _BagHeight + 7;//changes on dated 18th July 2017 as per dilen

                            }
                            SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght;
                        }
                    }
                    else
                    {
                        if (_Type == 0) //Internal
                        {
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 18;
                                if (comboBoxbottomtype.SelectedIndex == 2)
                                {
                                    if (comboConicalHeight.Text == "0")
                                        SideCutLenght = _BagHeight + (Convert.ToInt32(_BagWidth / 2)) + 18;
                                    else
                                        SideCutLenght = _BagHeight + Convert.ToInt32(comboConicalHeight.Text) + 18;
                                }
                                else
                                    SideCutLenght = _BagHeight + 18;
                            }
                            else
                            {
                                SideFabricSize = _BagWidth + 11; //changes on dated 18th July 2017 as per dilen
                                if (comboBoxbottomtype.SelectedIndex == 2)
                                {
                                    if (comboConicalHeight.Text == "0")
                                        SideCutLenght = _BagHeight + (Convert.ToInt32(_BagWidth / 2)) + 11;
                                    else
                                        SideCutLenght = _BagHeight + Convert.ToInt32(comboConicalHeight.Text) + 11; //changes on dated 18th July 2017 as per dilen
                                }
                                else
                                    SideCutLenght = _BagHeight + 11;//changes on dated 18th July 2017 as per dilen

                            }
                            SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght;
                            // + ((_BagLenght + 12) * (Utility.SafeConvertToDouble (comboSideGSM.Text) + Utility.SafeConvertToDouble (comboSideLamiGSM.Text)) * 2 * SideCutLenght);
                        }
                        else if (_Type == 1) //External
                        {
                            if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                            {
                                SideFabricSize = _BagWidth + 14;
                                if (comboBoxbottomtype.SelectedIndex == 2)
                                {
                                    if (comboConicalHeight.Text == "0")
                                        SideCutLenght = _BagHeight + (Convert.ToInt32(_BagWidth / 2)) + 14;
                                    else
                                        SideCutLenght = _BagHeight + Convert.ToInt32(comboConicalHeight.Text) + 14;
                                }
                                else
                                    SideCutLenght = _BagHeight + 14;
                            }
                            else
                            {
                                SideFabricSize = _BagWidth + 7; //changes on dated 18th July 2017 as per dilen
                                if (comboBoxbottomtype.SelectedIndex == 2)
                                {
                                    if (comboConicalHeight.Text == "0")
                                        SideCutLenght = _BagHeight + (Convert.ToInt32(_BagWidth / 2)) + 7;
                                    else
                                        SideCutLenght = _BagHeight + Convert.ToInt32(comboConicalHeight.Text) + 7; //changes on dated 18th July 2017 as per dilen
                                }
                                else
                                    SideCutLenght = _BagHeight + 7;//changes on dated 18th July 2017 as per dilen

                            }
                            SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght;

                            //+ ((_BagLenght + 8) * (Utility.SafeConvertToDouble (comboSideGSM.Text) + Utility.SafeConvertToDouble (comboSideLamiGSM.Text)) * 2 * SideCutLenght);
                        }
                    }
                }
            }



            if (_BodyIndex1 == 2) // Buffle 
            {
                if (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 6 || comboBuffleType.SelectedIndex == 1) //4 Panel
                {
                    if (comboLoopConst.SelectedIndex == 3) // Full Loop Cross Corner
                    {
                        if (textBodyL.Text == textBodyW.Text)
                        {
                            if (_Type == 0) //Internal
                            {
                                if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                                {
                                    SideFabricSize = _BagWidth + 4;
                                    SideCutLenght = _BagHeight + 5;
                                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                    {
                                        SideFabricSize = _BagWidth + 18;
                                        SideCutLenght = _BagHeight + 18;
                                    }
                                    SideWt = (SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght);

                                    //  + .1111 * (SideFabricSize * (Utility.SafeConvertToDouble (comboSideGSM.Text) + Utility.SafeConvertToDouble (comboSideLamiGSM.Text)) * 4 * SideCutLenght);
                                }
                                else
                                {
                                    SideFabricSize = _BagWidth + 11;
                                    SideCutLenght = _BagHeight + 11;
                                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                    {
                                        SideFabricSize = _BagWidth + 18;
                                        SideCutLenght = _BagHeight + 18;
                                    }
                                    SideWt = (SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght);

                                }
                            }
                            else if (_Type == 1) //External
                            {
                                if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                                {
                                    SideFabricSize = _BagWidth;
                                    SideCutLenght = _BagHeight + 1;
                                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                    {
                                        SideFabricSize = _BagWidth + 8;
                                        SideCutLenght = _BagHeight + 8;
                                    }
                                    SideWt = (SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght);
                                    // + .1111 * (SideFabricSize * (Utility.SafeConvertToDouble (comboSideGSM.Text) + Utility.SafeConvertToDouble (comboSideLamiGSM.Text)) * 4 * SideCutLenght);
                                }
                                else
                                {
                                    SideFabricSize = _BagWidth + 7;
                                    SideCutLenght = _BagHeight + 7;
                                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                    {
                                        SideFabricSize = _BagWidth + 14;
                                        SideCutLenght = _BagHeight + 14;
                                    }
                                    SideWt = (SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght);
                                }
                            }
                        }
                        else
                        {
                            if (_Type == 0) //Internal
                            {
                                if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                                {
                                    SideFabricSize = _BagWidth + 4;
                                    SideCutLenght = _BagHeight + 5;
                                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                    {
                                        SideFabricSize = _BagWidth + 18;
                                        SideCutLenght = _BagHeight + 18;
                                    }
                                    SideWt = (SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght);
                                    //    + .1111 * (SideFabricSize * (Utility.SafeConvertToDouble (comboSideGSM.Text) + Utility.SafeConvertToDouble (comboSideLamiGSM.Text)) * 2 * SideCutLenght);
                                }
                                else
                                {
                                    SideFabricSize = _BagWidth + 11;
                                    SideCutLenght = _BagHeight + 11;
                                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                    {
                                        SideFabricSize = _BagWidth + 18;
                                        SideCutLenght = _BagHeight + 18;
                                    }
                                    SideWt = (SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght);
                                }
                            }
                            else if (_Type == 1) //External
                            {
                                if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                                {
                                    SideFabricSize = _BagWidth;
                                    SideCutLenght = _BagHeight + 1;
                                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                    {
                                        SideFabricSize = _BagWidth + 8;
                                        SideCutLenght = _BagHeight + 8;
                                    }
                                    SideWt = (SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght);
                                    //   + .1111 * (SideFabricSize * (Utility.SafeConvertToDouble (comboSideGSM.Text) + Utility.SafeConvertToDouble (comboSideLamiGSM.Text)) * 2 * SideCutLenght);
                                }
                                else
                                {

                                    SideFabricSize = _BagWidth + 7;
                                    SideCutLenght = _BagHeight + 7;
                                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                    {
                                        SideFabricSize = _BagWidth + 14;
                                        SideCutLenght = _BagHeight + 14;
                                    }
                                    SideWt = (SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_Type == 0) //Internal
                        {
                            if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                            {
                                SideFabricSize = _BagWidth + 4;
                               // SideCutLenght = _BagHeight + 5; comment by manish on 22nd July 2022 
                                SideCutLenght = _BagHeight + 11;  
                                
                                if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                {
                                    SideFabricSize = _BagWidth + 12;
                                    SideCutLenght = _BagHeight + 12;
                                }
                                SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght;
                            }
                            else
                            {
                                SideFabricSize = _BagWidth + 11;
                                SideCutLenght = _BagHeight + 11;
                                if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                {
                                    SideFabricSize = _BagWidth + 18;
                                    SideCutLenght = _BagHeight + 18;
                                }
                                SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght;
                            }
                        }
                        else if (_Type == 1) //External
                        {
                            if (comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Ventilated && Sulzer  
                            {
                                SideFabricSize = _BagWidth;
                                SideCutLenght = _BagHeight + 7;
                                if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                {
                                    SideFabricSize = _BagWidth + 8;
                                    SideCutLenght = _BagHeight + 8;
                                }
                                SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght;
                            }
                            else
                            {
                                SideFabricSize = _BagWidth + 7;
                                SideCutLenght = _BagHeight + 7;
                                if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                                {
                                    SideFabricSize = _BagWidth + 14;
                                    SideCutLenght = _BagHeight + 14;
                                }
                                SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 2 * SideCutLenght;

                            }
                        }
                    }
                }
                else if (comboBuffleType.SelectedIndex == 2) //Tube + Corner + Buffle
                {
                    if (_Type == 0) //Internal
                    {
                        SideFabricSize = (_BagLenght / 3) + (_BagWidth / 3) + 16;
                        SideCutLenght = _BagHeight + 12;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = _BagWidth + 18;
                            SideCutLenght = _BagHeight + 18;
                        }
                        SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght;
                    }
                    else if (_Type == 1) //External
                    {
                        SideFabricSize = ((_BagLenght - 5) / 3) + ((_BagWidth - 5) / 3) + 12;
                        SideCutLenght = _BagHeight + 8;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = ((_BagLenght - 5) / 3) + ((_BagWidth - 5) / 3) + 19;
                            SideCutLenght = _BagHeight + 15;
                        }
                        SideWt = SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght;
                    }
                }
                else if (comboBuffleType.SelectedIndex == 3) //Tube + CrossCorner + Buffle
                {
                    if (_Type == 0) //Internal
                    {
                        SideFabricSize = (_BagLenght / 3) + (_BagWidth / 3) + 12;
                        SideCutLenght = _BagHeight + 12;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = (_BagLenght / 3) + (_BagWidth / 3) + 18;
                            SideCutLenght = _BagHeight + 18;
                        }
                        SideWt = (SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght);
                        /// + .1111 * (SideFabricSize * (Utility.SafeConvertToDouble (comboSideGSM.Text) + Utility.SafeConvertToDouble (comboSideLamiGSM.Text)) * 4 * SideCutLenght);
                    }
                    else if (_Type == 1) //External
                    {
                        SideFabricSize = ((_BagLenght - 5) / 3) + ((_BagWidth - 5) / 3) + 8;
                        SideCutLenght = _BagHeight + 8;
                        if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                        {
                            SideFabricSize = ((_BagLenght - 5) / 3) + ((_BagWidth - 5) / 3) + 15;
                            SideCutLenght = _BagHeight + 15;
                        }
                        SideWt = (SideFabricSize * (_BagSideGSM + _BagSideLamiGSM) * 4 * SideCutLenght);
                        //+ .1111 * (SideFabricSize * (Utility.SafeConvertToDouble (comboSideGSM.Text) + Utility.SafeConvertToDouble (comboSideLamiGSM.Text)) * 4 * SideCutLenght);
                    }
                }
                else
                {
                    SideCutLenght = BodyCutLenght;
                    SideFabricSize = BodyFabricSize;
                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                    {
                        SideFabricSize = _BagWidth + 18;
                        SideCutLenght = _BagHeight + 18;
                    }
                    SideWt = BodyWt;
                }

            }

            if (_BodyIndex1 == 1) // Circular
            {
                SideCutLenght = BodyCutLenght;
                SideFabricSize = BodyFabricSize;
                if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                {
                    SideFabricSize = _BagWidth + 18;
                    SideCutLenght = _BagHeight + 18;
                }
                SideWt = BodyWt;
            }

            if (_BodyIndex1 == 11)  // Hood Bag/Covered Bag
            {
                if (_Type == 0) //internal
                {
                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                    {
                        SideFabricSize = _BagWidth + 18;
                        if (comboBody2.SelectedIndex == 10 || comboBody2.SelectedIndex == 11)
                            SideCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(comboHoodSIze.Text) + 18;
                        else
                        {
                            SideCutLenght = (_BagHeight + 18) * (SideFabricSize + 18) * 2;
                            SideCutLenght = SideCutLenght / 100;
                        }
                        SideCutLenght = Math.Round(SideCutLenght, 2);
                    }
                    else
                    {
                        SideFabricSize = BodyFabricSize;

                        if (comboBody2.SelectedIndex == 10 || comboBody2.SelectedIndex == 11)
                            SideCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(comboHoodSIze.Text) + 14;
                        else
                        {
                            SideCutLenght = (_BagHeight + 12) * (SideFabricSize + 12) * 2;
                            SideCutLenght = SideCutLenght / 100;
                        }
                        SideCutLenght = Math.Round(SideCutLenght, 2);
                    }
                    SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize;
                }
                else
                {
                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                    {
                        SideFabricSize = _BagWidth + 18;
                        if (comboBody2.SelectedIndex == 10 || comboBody2.SelectedIndex == 11)
                            SideCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(comboHoodSIze.Text) + 15;
                        else
                            SideCutLenght = (_BagHeight + 15) * (SideFabricSize + 15) * 2;
                        SideCutLenght = SideCutLenght / 100;
                        SideCutLenght = Math.Round(SideCutLenght, 2);
                    }
                    else
                    {
                        SideFabricSize = BodyFabricSize;
                        if (comboBody2.SelectedIndex == 10 || comboBody2.SelectedIndex == 11)
                            SideCutLenght = (_BagHeight * 2) + Utility.SafeConvertToDouble(comboHoodSIze.Text) + 8;
                        else
                            SideCutLenght = (_BagHeight + 8) * (SideFabricSize + 8) * 2;
                        SideCutLenght = SideCutLenght / 100;
                        SideCutLenght = Math.Round(SideCutLenght, 2);
                    }
                    SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM) * SideFabricSize;
                }
            }
            if (_BodyIndex1 == 12) // Double Layer Tunnel Lift Loop Bag
            {
                if (_Type == 0) //internal
                {
                    SideFabricSize = _BagLenght + 12;
                    SideCutLenght = (_BagHeight + 12);
                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                    {
                        SideFabricSize = _BagWidth + 18;
                        SideCutLenght = _BagHeight + 18;
                    }
                    SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM)
                        * SideFabricSize * 2;
                }
                else
                {
                    SideFabricSize = _BagLenght + 8;
                    SideCutLenght = _BagHeight + 8;
                    if (checkBoxDoubleFoldBody.Checked) //20.08.2021 add Double Fold
                    {
                        SideFabricSize = _BagWidth + 15;
                        SideCutLenght = _BagHeight + 15;
                    }
                    SideWt = SideCutLenght * (_BagSideGSM + _BagSideLamiGSM)
                        * SideFabricSize * 2;
                }
            }
            if (checkBoxRF.Checked)
                SideWt += SideWt * 0.1111;

            if (textSideNo.Text != "")
                SideWt = SideWt * Convert.ToInt32(textSideNo.Text);

            return SideWt;
        }
        private void BaseWtFormula()
        {
            if (checkBoxbottomlam.Checked == false)
                comboBottomLamiGSM.Text = "0";
            

            if (_BodyIndex1 == 5 || _BodyIndex1 == 6 || _BodyIndex1 == 1) // Single Loop & Double Loop & circular
            {
                if ((_BodyIndex1 == 6 || _BodyIndex1 == 5) && (comboBoxbottomtype.SelectedIndex == 5))
                    return;
                if (_BodyIndex1 == 5 || _BodyIndex1 == 6)
                {
                    BaseFabricSize = (_BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : 10));
                    BaseCutLenght = (_BagWidth + (checkBoxDoubleFoldBottom.Checked ? 18 : 10));
                }
                else
                {
                    BaseFabricSize = (_BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : 12));
                    BaseCutLenght = (_BagWidth + (checkBoxDoubleFoldBottom.Checked ? 18 : 12));
                }
                //if (checkBoxDoubleFoldBottom.Checked) //20.08.2021 add Double Fold
                //{
                //    BaseFabricSize = _BagLenght + 18;
                //    BaseCutLenght = _BagWidth + 18;
                //}
                double TotalGSM = (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text));
                BaseWt = BaseCutLenght * BaseFabricSize * TotalGSM;
            }
            else if (_BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4
                || _BodyIndex1 == 7 || _BodyIndex1 == 8)
            {

                if (_Type == 0) //Internal
                {
                    if (comboBody2.SelectedIndex == 12) //|| comboBody2.SelectedIndex == 13) //10.08.2021
                    {
                        BaseFabricSize = (_BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : 12));
                        BaseCutLenght = (_BagHeight + (checkBoxDoubleFoldBottom.Checked ? 18 : 12));
                    }
                    else
                    {
                        BaseFabricSize = (_BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : 12));
                        BaseCutLenght = (_BagWidth + (checkBoxDoubleFoldBottom.Checked ? 18 : 12));
                        //  BaseWt = BaseCutLenght * BaseFabricSize * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text));
                    }
                }
                else if (_Type == 1) //External
                {
                    if (comboBody2.SelectedIndex == 12)// || comboBody2.SelectedIndex == 13) //10.08.2021
                    {
                        if (checkBoxDoubleFoldBottom.Checked) //20.08.2021 add Double Fold
                        {
                            BaseFabricSize = _BagLenght + 15; //change to 15 from 18 as added only 7 from previous formula of single fold
                            BaseCutLenght = _BagWidth + 15;
                        }
                        else
                        {
                            BaseFabricSize = _BagLenght + 8;
                            BaseCutLenght = _BagHeight + 8;
                        }
                    }
                    else
                    {

                        if (checkBoxDoubleFoldBottom.Checked) //20.08.2021 add Double Fold
                        {
                            BaseFabricSize = _BagLenght + 15; //change to 15 from 18 as added only 7 from previous formula of single fold
                            BaseCutLenght = _BagWidth + 15;
                        }
                        else
                        {
                            BaseFabricSize = _BagLenght + 8;
                            BaseCutLenght = _BagWidth + 8;
                        }
                    }
                }

                if (comboBody2.SelectedIndex == 12 || comboBody2.SelectedIndex == 13) //Almatis [Wider Fold] /Almatis [Std Fold] 19.06.2021
                {
                    BaseWt = BaseCutLenght * BaseFabricSize * 4 * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text));
                }
                else
                {
                    BaseWt = BaseCutLenght * BaseFabricSize * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text));
                }
            }

            else if (_BodyIndex1 == 9) //Conical Bag Three Piece
            {
                double OneSideDia = (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) * 3.14) / 4;
                if (_Type == 0) //Internal
                {
                    BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : 12); //20.08.2021 add Double Fold
                    BaseCutLenght = ((_BagWidth - OneSideDia) / 2) + (checkBoxDoubleFoldBottom.Checked ? 18 : 14);//20.08.2021 add Double Fold
                    //BaseWt = BaseCutLenght * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text)) * 4 * BaseFabricSize;
                }
                else if (_Type == 1) //External
                {
                    BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 15 : 8);//20.08.2021 add Double Fold
                    BaseCutLenght = ((_BagWidth - OneSideDia) / 2) + (checkBoxDoubleFoldBottom.Checked ? 17 : 10);//20.08.2021 add Double Fold
                }
                BaseWt = BaseCutLenght * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text)) * 4 * BaseFabricSize;
            }

            if (_BodyIndex1 == 1 || _BodyIndex1 == 2
                    || _BodyIndex1 == 3 || _BodyIndex1 == 4 || _BodyIndex1 == 9)
            {
                if (comboBoxbottomtype.SelectedIndex == 1) //Conical Plate
                {
                    if (_Type == 0) //Internal
                    {

                        BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : Convert.ToDouble(comboConicalHeight.Text));
                        BaseCutLenght = _BagWidth + (checkBoxDoubleFoldBottom.Checked ? 18 : Convert.ToDouble(comboConicalHeight.Text));

                        //BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : 32);
                        //BaseCutLenght = _BagWidth + (checkBoxDoubleFoldBottom.Checked ? 18 : 32);

                    }
                    else if (_Type == 1) //External
                    {
                        BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : (Convert.ToDouble(comboConicalHeight.Text) - 4));
                        BaseCutLenght = _BagWidth + (checkBoxDoubleFoldBottom.Checked ? 18 : (Convert.ToDouble(comboConicalHeight.Text) - 4));

                        //BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : 28);
                        //BaseCutLenght = _BagWidth + (checkBoxDoubleFoldBottom.Checked ? 18 : 28);
                    }

                    BaseWt = BaseCutLenght * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text)) * BaseFabricSize;
                }
                else if (comboBoxbottomtype.SelectedIndex == 2) //Conical Bottom
                {
                    double OneSideDia = (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) * 3.14) / 4;
                    if (_Type == 0) //Internal
                    {
                        BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : 12);
                        BaseCutLenght = ((_BagWidth - OneSideDia) / 2) + (checkBoxDoubleFoldBottom.Checked ? 18 : 14);
                        //    BaseWt = BaseCutLenght * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text)) * 4 * BaseFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 15 : 8);
                        BaseCutLenght = ((_BagWidth - OneSideDia) / 2) + (checkBoxDoubleFoldBottom.Checked ? 18 : 10);
                    }
                    BaseWt = BaseCutLenght * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text)) * 4 * BaseFabricSize;
                }
            }
            if (_BodyIndex1 == 12) //Double Layer Tunnel Lift Loop Bag
            {
                if (_Type == 0) //Internal
                {
                    BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : 12);
                    BaseCutLenght = _BagWidth + (checkBoxDoubleFoldBottom.Checked ? 18 : 12);
                    //    BaseWt = BaseCutLenght * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text)) * BaseFabricSize;
                }
                else if (_Type == 1) //External
                {
                    BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 15 : 8);
                    BaseCutLenght = _BagWidth + (checkBoxDoubleFoldBottom.Checked ? 15 : 8);
                }

                BaseWt = BaseCutLenght * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text)) * BaseFabricSize;
            }
            if (_BodyIndex1 == 13)
            {
                BaseFabricSize = (_BagLenght + (checkBoxDoubleFoldBottom.Checked ? 15 : 12));
                BaseCutLenght = (_BagWidth + (checkBoxDoubleFoldBottom.Checked ? 15 : 12));
                BaseWt = BaseCutLenght * BaseFabricSize * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text));
            }

            if (_BodyIndex1 == 11)
            {
                if (comboBody2.SelectedIndex == 11)
                {
                    if (_Type == 0) //Internal
                    {
                        BaseFabricSize = (_BagLenght + (checkBoxDoubleFoldBottom.Checked ? 18 : 12));
                        BaseCutLenght = (_BagWidth + (checkBoxDoubleFoldBottom.Checked ? 18 : 12));
                        //  BaseWt = BaseCutLenght * BaseFabricSize * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text));
                    }
                    else if (_Type == 1) //External
                    {
                        BaseFabricSize = _BagLenght + (checkBoxDoubleFoldBottom.Checked ? 15 : 8);
                        BaseCutLenght = _BagWidth + (checkBoxDoubleFoldBottom.Checked ? 15 : 8);
                    }
                    BaseWt = BaseCutLenght * BaseFabricSize * (Utility.SafeConvertToDouble(comboBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboBoxbottomgsm.Text));
                }
            }
            BaseWt = BaseWt * Utility.SafeConvertToDouble(textBottomNo.Text);
        }
        private void DuffleWtFormula() // It means Skrit
        {
            if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 7
                || comboTopType.SelectedIndex == 8) //17.06.2021
            {
                if (_BodyIndex1 == 0 || _BodyIndex1 == 3) //18.06.2021
                {
                    if (comboTopType.SelectedIndex == 8) //17.06.2021
                    {
                        DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 18 : 15);
                    }
                    else
                    {
                        if (_Type == 0) //Internal
                        {
                            DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 12 : 5);
                            //DuffleCutLenght = (_BagLenght * 4) + (checkBoxDoubleFoldTop.Checked ? 14 : 12); ////14 from 18 24.08.2021
                            DuffleCutLenght = ((_BagLenght + _BagWidth) * 2) + (checkBoxDoubleFoldTop.Checked ? 14 : 12); ////14 from 18 24.08.2021
                            //((_BagLenght + _BagWidth) * 2)
                        }
                        if (_Type == 1) //External
                        {
                            DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 12 : 5);
                            //DuffleCutLenght = ((_BagLenght - 4) * 4) + (checkBoxDoubleFoldTop.Checked ? 14 : 12); ////14 from 18 24.08.2021
                            DuffleCutLenght = (((_BagLenght - 4) + (_BagWidth - 4)) * 2) + (checkBoxDoubleFoldTop.Checked ? 14 : 12); ////14 from 18 24.08.2021
                        }
                    }
                }
                else
                {
                    if (_BodyIndex1 == 1) //18.06.2021
                    {
                        if (comboTopType.SelectedIndex == 8) //17.06.2021
                        {
                            DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 18 : 15);
                        }
                        else
                        {
                            if (checkBoxTopLam.Checked)
                            {
                                if (comboBody3.SelectedIndex == 2) //fda
                                    DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 12 : 10); //12 from 18 24.08.2021
                                else
                                    DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 12 : 5); //12 from 18 24.08.2021
                            }
                            else
                                DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 12 : 12);//12 from 18 24.08.2021
                        }
                        //DuffleCutLenght = (_BagLenght * 4) + (checkBoxDoubleFoldTop.Checked ? 14 : 12);//14 from 18 24.08.2021
                        DuffleCutLenght = ((_BagLenght + _BagWidth) * 2) + (checkBoxDoubleFoldTop.Checked ? 14 : 12);//14 from 18 24.08.2021
                    }
                    else
                    {
                        if (comboTopType.SelectedIndex == 8) //17.06.2021
                        {
                            DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 18 : 15);
                        }
                        else
                        {
                            if (checkBoxTopLam.Checked)
                            {
                                if (comboBody3.SelectedIndex == 2) //fda
                                    DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 18 : 10);
                                else
                                    DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 18 : 5);
                            }
                            else
                                DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 18 : 12);
                        }
                        if (_Type == 0) //Internal
                        {
                            // DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + 5;
                            DuffleCutLenght = ((_BagLenght + _BagWidth) * 2) + (checkBoxDoubleFoldTop.Checked ? 18 : 12);
                            //   DuffleWt = DuffleCutLenght * DuffleFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                        }
                        if (_Type == 1) //External
                        {
                            DuffleCutLenght = (((_BagLenght - 4) + (_BagWidth - 4)) * 2) + (checkBoxDoubleFoldTop.Checked ? 18 : 12);
                        }
                    }
                }

            }
            else if (comboTopType.SelectedIndex == 6 || comboTopType.SelectedIndex == 9)//LENO -- added by Rikin on 11-02-2015 discussed with ajay 
            {
                // No internal and external in this kind of bAG
                DuffleFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 18 : 10);
                DuffleCutLenght = ((_BagLenght + _BagWidth) * 2) + (checkBoxDoubleFoldTop.Checked ? 23 : 16);
                //DuffleWt = DuffleCutLenght * DuffleFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
            }
            DuffleWt = DuffleCutLenght * DuffleFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));

        }
        private void BottomDuffleWtFormula() // It means Bottom Skrit
        {
            if (comboBoxbottomtype.SelectedIndex == 8)
            {
                BottomDuffleFabricSize = Utility.SafeConvertToDouble(textSkirtHeight.Text) + (checkBoxDoubleFoldBottom.Checked ? 18 : 5);
                if (_Type == 0) //Internal
                {
                    BottomDuffleCutLenght = ((_BagLenght + _BagWidth) * 2) + (checkBoxDoubleFoldBottom.Checked ? 18 : 12);
                    //  BottomDuffleWt = BottomDuffleCutLenght * BottomDuffleFabricSize * (Utility.SafeConvertToDouble(comboBoxbottomgsm.Text) + Utility.SafeConvertToDouble(comboBottomLamiGSM.Text));
                }
                if (_Type == 1) //External
                {
                    // BottomDuffleFabricSize = Utility.SafeConvertToDouble(textSkirtHeight.Text) + 5;
                    BottomDuffleCutLenght = (((_BagLenght - 4) + (_BagWidth - 4)) * 2) + (checkBoxDoubleFoldBottom.Checked ? 18 : 12);
                }
                BottomDuffleWt = BottomDuffleCutLenght * BottomDuffleFabricSize * (Utility.SafeConvertToDouble(comboBoxbottomgsm.Text) + Utility.SafeConvertToDouble(comboBottomLamiGSM.Text));
            }
        }
        private void TopWtFormula()
        {
            if ((checkBoxTop.Checked))
            {

                if (checkBoxTopLam.Checked == false)
                {
                    comboTopLamiGSM.Text = "0";
                }

                if (_BodyIndex1 == 5 || _BodyIndex1 == 6 || _BodyIndex1 == 1) // Single Loop & Double Loop & circular
                {
                    TopFabricSize = (_BagLenght + 12);
                    TopCutLenght = (_BagWidth + 12);
                    if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                    {
                        TopFabricSize = _BagLenght + 18;
                        TopCutLenght = _BagWidth + 18;
                    }
                    TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                }
                else if (_BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4 || _BodyIndex1 == 7 || _BodyIndex1 == 8)
                {
                    if (_Type == 0) //Internal
                    {
                        if (comboBody2.SelectedIndex == 12 || comboBody2.SelectedIndex == 13)//Almatis [Wider Fold] & [Std Fold] 19.06.2021; 10.08.2021 chagne _bagheight to _bagwidth as per email from mustak
                        {
                            TopFabricSize = (_BagLenght + 15);
                            TopCutLenght = (_BagWidth + 15);// (_BagHeight + 15);10.08.2021
                        }
                        else
                        {
                            TopFabricSize = (_BagLenght + 12);
                            TopCutLenght = (_BagWidth + 12);
                        }
                        if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                        {
                            TopFabricSize = _BagLenght + 18;
                            TopCutLenght = _BagWidth + 18;
                        }
                        // TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                    }
                    else if (_Type == 1) //External
                    {
                        if (comboBody2.SelectedIndex == 12 || comboBody2.SelectedIndex == 13)//Almatis [Wider Fold] & [Std Fold] 19.06.2021 ; 10.08.2021 chagne _bagheight to _bagwidth as per email from mustak
                        {
                            TopFabricSize = (_BagLenght + (checkBoxDoubleFoldTop.Checked ? 18 : 11));//18.08.2021 add Double Fold
                            TopCutLenght = (_BagWidth + (checkBoxDoubleFoldTop.Checked ? 18 : 11));//(_BagHeight + 11);10.08.2021
                        }
                        else
                        {
                            TopFabricSize = _BagLenght + (checkBoxDoubleFoldTop.Checked ? 15 : 8);//18.08.2021 add Double Fold
                            TopCutLenght = _BagWidth + (checkBoxDoubleFoldTop.Checked ? 15 : 8);//18.08.2021 add Double Fold
                        }
                        //if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                        //{
                        //    TopFabricSize = _BagLenght + 18;
                        //    TopCutLenght = _BagWidth + 18;
                        //}
                    }
                    if (comboBody2.SelectedIndex == 12 || comboBody2.SelectedIndex == 13)//Almatis [Wider Fold] & [Std Fold] 19.06.2021
                    {
                        TopWt = TopCutLenght * TopFabricSize * 4 * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                    }
                    else
                    {
                        TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                    }
                }
                else if (_BodyIndex1 == 9) //Conical Bag Three Piece
                {
                    double OneSideDia = (Utility.SafeConvertToDouble(comboSpoutDia.Text) * 3.14) / 4;
                    if (_Type == 0) //Internal
                    {
                        TopFabricSize = _BagLenght + (checkBoxDoubleFoldTop.Checked ? 18 : 12);//18.08.2021 add Double Fold
                        TopCutLenght = ((_BagWidth - OneSideDia) / 2) + (checkBoxDoubleFoldTop.Checked ? 18 : 14);//18.08.2021 add Double Fold
                        //  TopWt = TopCutLenght * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text)) * 4 * TopFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        TopFabricSize = _BagLenght + (checkBoxDoubleFoldTop.Checked ? 15 : 8);//18.08.2021 add Double Fold
                        TopCutLenght = ((_BagWidth - OneSideDia) / 2) + (checkBoxDoubleFoldTop.Checked ? 18 : 10);//18.08.2021 add Double Fold

                    }
                    //if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                    //{
                    //    TopFabricSize = _BagLenght + 18;
                    //    TopCutLenght = _BagWidth + 18;
                    //}
                    TopWt = TopCutLenght * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text)) * 4 * TopFabricSize;
                }

                if (_BodyIndex1 == 0 || _BodyIndex1 == 1 || _BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4 || _BodyIndex1 == 9 || _BodyIndex1 == 11)
                {

                    if (comboBody2.SelectedIndex == 11)
                    {
                        if (_Type == 0) //Internal
                        {
                            TopFabricSize = (_BagLenght + (checkBoxDoubleFoldTop.Checked ? 18 : 12));
                            TopCutLenght = (_BagWidth + (checkBoxDoubleFoldTop.Checked ? 18 : 12));
                            // TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                        }
                        else if (_Type == 1) //External
                        {
                            TopFabricSize = _BagLenght + (checkBoxDoubleFoldTop.Checked ? 15 : 8);
                            TopCutLenght = _BagWidth + (checkBoxDoubleFoldTop.Checked ? 15 : 8);

                        }
                        //if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                        //{
                        //    TopFabricSize = _BagLenght + 18;
                        //    TopCutLenght = _BagWidth + 18;
                        //}
                        TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                    }
                    else
                    {
                        if (comboTopType.SelectedIndex == 3) //Conical Plate
                        {
                            if (_Type == 0) //Internal
                            {
                                TopFabricSize = _BagLenght + Convert.ToDouble(textConicaltop.Text);// comment as per discussion with dilen
                                TopCutLenght = _BagWidth + Convert.ToDouble(textConicaltop.Text);
                                if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                                {
                                    TopFabricSize = _BagLenght + Convert.ToDouble(textConicaltop.Text) + 18;
                                    TopCutLenght = _BagWidth + Convert.ToDouble(textConicaltop.Text) + 18;
                                }

                                // TopWt = TopCutLenght * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text)) * TopFabricSize;
                            }
                            else if (_Type == 1) //External
                            {
                                TopFabricSize = _BagLenght + Convert.ToDouble(textConicaltop.Text);
                                TopCutLenght = _BagWidth + Convert.ToDouble(textConicaltop.Text);
                                if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                                {
                                    TopFabricSize = _BagLenght + Convert.ToDouble(textConicaltop.Text) + 18;
                                    TopCutLenght = _BagWidth + Convert.ToDouble(textConicaltop.Text) + 18;
                                }
                            }
                            TopWt = TopCutLenght * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text)) * TopFabricSize;
                        }
                        else if (comboTopType.SelectedIndex == 4) //Conical Top
                        {
                            #region Change on 17.6.2021 as per email
                            //double OneSideDia = (Utility.SafeConvertToDouble(comboSpoutDia.Text) * 3.14) / 4;
                            //if (_Type == 0) //Internal
                            //{
                            //    TopFabricSize = _BagLenght + 12;
                            //    TopCutLenght = ((_BagWidth - OneSideDia) / 2) + 14;
                            //    // TopWt = TopCutLenght * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text)) * 4 * TopFabricSize;
                            //}
                            //else if (_Type == 1) //External
                            //{
                            //    TopFabricSize = _BagLenght + 8;
                            //    TopCutLenght = ((_BagWidth - OneSideDia) / 2) + 10;

                            //}
                            //TopWt = TopCutLenght * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text)) * 4 * TopFabricSize;
                            #endregion
                            if (_Type == 0) //Internal
                            {
                                TopFabricSize = _BagLenght + 12 + (Convert.ToInt32(textConicaltop.Text) * 2);
                                TopCutLenght = _BagLenght + 12 + (Convert.ToInt32(textConicaltop.Text) * 2);
                                if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                                {
                                    TopFabricSize = _BagLenght + 18 + (Convert.ToInt32(textConicaltop.Text) * 2);
                                    TopCutLenght = _BagLenght + 18 + (Convert.ToInt32(textConicaltop.Text) * 2);
                                }
                                TopWt = TopCutLenght * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text)) * TopFabricSize;
                            }
                            else if (_Type == 1) //External
                            {
                                TopFabricSize = _BagLenght + 8 + (Convert.ToInt32(textConicaltop.Text) * 2);
                                TopCutLenght = _BagLenght + 8 + (Convert.ToInt32(textConicaltop.Text) * 2);
                                if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                                {
                                    TopFabricSize = _BagLenght + 14 + (Convert.ToInt32(textConicaltop.Text) * 2);
                                    TopCutLenght = _BagLenght + 14 + (Convert.ToInt32(textConicaltop.Text) * 2);
                                }
                                TopWt = TopCutLenght * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text)) * TopFabricSize;
                            }
                        }
                        else if (comboTopType.SelectedIndex == 1) //TopSpout
                        {
                            if (_Type == 0) //Internal
                            {
                                if (comboBody2.SelectedIndex == 12 || comboBody2.SelectedIndex == 13)//Almatis [Wider Fold] & [Std Fold] 19.06.2021
                                {
                                    TopFabricSize = (_BagLenght + 15);
                                    TopCutLenght = (_BagWidth + 15); //(_BagHeight + 15); 10.08.2021
                                }
                                else
                                {
                                    TopFabricSize = (_BagLenght + 12);
                                    TopCutLenght = (_BagWidth + 12);
                                }
                                if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                                {
                                    TopFabricSize = _BagLenght + 18;
                                    TopCutLenght = _BagWidth + 18;
                                }
                                //TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                            }
                            else if (_Type == 1) //External
                            {
                                if (comboBody2.SelectedIndex == 12 || comboBody2.SelectedIndex == 13)//Almatis [Wider Fold] & [Std Fold] 19.06.2021
                                {
                                    TopFabricSize = (_BagLenght + 11);
                                    TopCutLenght = (_BagWidth + 11); //(_BagHeight + 11); 10.08.2021
                                }
                                else
                                {
                                    TopFabricSize = _BagLenght + 8;
                                    TopCutLenght = _BagWidth + 8;
                                }
                                if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                                {
                                    TopFabricSize = _BagLenght + 18;
                                    TopCutLenght = _BagWidth + 18;
                                }
                            }
                            if (comboBody2.SelectedIndex == 12 || comboBody2.SelectedIndex == 13)//Almatis [Wider Fold] & [Std Fold] 19.06.2021
                            {
                                TopWt = TopCutLenght * TopFabricSize * 4 * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                            }
                            else
                            {
                                TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                            }
                        }
                        else if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 6 ||
                            comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8 || comboTopType.SelectedIndex == 9) //Duffle 17.06.2021
                        {
                            if (_Type == 0) //Internal
                            {
                                if (comboTopType.SelectedIndex == 8)
                                {
                                    TopFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 12 : 15); //12 from 18 24/08/2021
                                }
                                else
                                {
                                    TopFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 12 : 5); //12 from 18 24/08/2021
                                }
                                TopCutLenght = (_BagLenght * 4) + (checkBoxDoubleFoldTop.Checked ? 14 : 12);//14 from 18 24/08/2021
                                //if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                                //{
                                //    TopFabricSize = _BagLenght + 18;
                                //    TopCutLenght = _BagWidth + 18;
                                //}
                                // TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                            }
                            else if (_Type == 1) //External
                            {
                                if (comboTopType.SelectedIndex == 8)
                                {
                                    TopFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 12 : 15);
                                }
                                else
                                    TopFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + (checkBoxDoubleFoldTop.Checked ? 12 : 5);
                                TopCutLenght = ((_BagLenght - 4) * 4) + (checkBoxDoubleFoldTop.Checked ? 14 : 12); //18 from 14
                            }

                            TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                        }
                    }
                }
                if (_BodyIndex1 == 13)
                {
                    if (comboTopType.SelectedIndex == 3) //Conical Plate
                    {
                        TopFabricSize = _BagLenght + 32;
                        TopCutLenght = _BagWidth + 32;
                        if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                        {
                            TopFabricSize = _BagLenght + 18;
                            TopCutLenght = _BagWidth + 18;
                        }
                        TopWt = TopCutLenght * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text)) * TopFabricSize;
                    }
                    else if (comboTopType.SelectedIndex == 4) //Conical Top
                    {
                        double OneSideDia = (Utility.SafeConvertToDouble(comboSpoutDia.Text) * 3.14) / 4;
                        if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                        {
                            TopFabricSize = _BagLenght + 18;
                            TopCutLenght = ((_BagWidth - OneSideDia) / 2) + 18;
                        }
                        else
                        {
                            TopFabricSize = _BagLenght + 12;
                            TopCutLenght = ((_BagWidth - OneSideDia) / 2) + 14;
                        }
                        TopWt = TopCutLenght * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text)) * 4 * TopFabricSize;
                    }
                    else if (comboTopType.SelectedIndex == 1) //TopSpout
                    {
                        TopFabricSize = (_BagLenght + 12);
                        TopCutLenght = (_BagWidth + 12);
                        if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                        {
                            TopFabricSize = _BagLenght + 18;
                            TopCutLenght = _BagWidth + 18;
                        }
                        TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));

                    }
                    else if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 6 ||
                        comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8 || comboTopType.SelectedIndex == 9) //Duffle 17.06.2021
                    {
                        if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                        {
                            if (comboTopType.SelectedIndex == 8)
                                TopFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + 18;
                            else
                                TopFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + 18;
                            TopCutLenght = (_BagLenght * 4) + 18;
                        }
                        else
                        {
                            if (comboTopType.SelectedIndex == 8)
                                TopFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + 15;
                            else
                                TopFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + 5;
                            TopCutLenght = (_BagLenght * 4) + 12;
                        }
                        TopWt = TopCutLenght * TopFabricSize * (Utility.SafeConvertToDouble(comboBoxTopGSM.Text) + Utility.SafeConvertToDouble(comboTopLamiGSM.Text));
                    }
                }

                if (textTopNo.Text == "")
                    textTopNo.Text = "1";
                TopWt = TopWt * Convert.ToDouble(textTopNo.Text);
            }
        }
        private void LinerWtFormula()
        {
            int _TopType = comboTopType.SelectedIndex;
            int _BottomType = comboBoxbottomtype.SelectedIndex;

            double Density = 1;
            if (comboBoxlinertype.SelectedIndex == 1 || comboBoxlinertype.SelectedIndex == 2) //LD or 
                Density = .92;
            else if (comboBoxlinertype.SelectedIndex == 2) //HD
                Density = .94;
            else if (comboBoxlinertype.SelectedIndex == 4) //alu
                Density = 1.1;
            if (comboBoxlinerwidth.SelectedIndex != 0 || comboBoxlinerwidth.Text != "0")
            {
                LinerFabricSize = Utility.SafeConvertToDouble(comboBoxlinerwidth.Text);
                LinerCutLenght = Utility.SafeConvertToDouble(comboBoxlinerheight.Text);
                LinerWt = LinerCutLenght * LinerFabricSize * 2 * Utility.SafeConvertToDouble(comboBoxlinermicron.Text) * Density;
            }
            else
            {
                //By Rikin on 20-Apr-2015 as discussed with Dilen ji
                if (checkBoxlinerBuffle.Checked)
                {
                    LinerFabricSizeBuffle = Math.Round(Math.Sqrt(((_BagLenght + 2.5) / 3) * ((_BagLenght + 2.5) / 3)
                             + ((_BagWidth + 2.5) / 3) * ((_BagWidth + 2.5) / 3)) + 6, 0);
                    LinerCutLenghtBuffle = _BagHeight - 20;
                    LinerBuffleWt = (LinerCutLenghtBuffle * LinerFabricSizeBuffle * Utility.SafeConvertToDouble(textBuffleLinerMicron.Text) * Density) * 4;
                }

                double Bottom = (((_BagLenght / 2) + (_BagWidth / 2)) / 2);
                double Height = _BagHeight;
                if (_Type == 1)
                {
                    _BagLenght = _BagLenght - 4;
                    _BagWidth = _BagWidth - 4;
                }
                // double LinerHt = (Math.Sqrt((_BagLenght * _BagLenght) + (_BagWidth * _BagWidth)) / 2);
                double SkritHt;
                if (comboBoxduffleskirtheight.Text.Length > 0)
                    SkritHt = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text);
                else SkritHt = 0;

                double DSHt = Utility.SafeConvertToDouble(comboBoxbottomheight.Text);
                double FSHt = Utility.SafeConvertToDouble(comboSpoutHeight.Text);
                double BottomSpoutDia = 0;
                double TopSpoutDia = 0;
                if (DSHt != 0)
                    BottomSpoutDia = Math.Round(((((_BagLenght + _BagWidth + 5) - (1.57 * Utility.SafeConvertToDouble(comboBoxbottomdia.Text) + 3)) / 4) * 1.12), 0);
                if (FSHt != 0)
                    TopSpoutDia = Math.Round(((((_BagLenght + _BagWidth + 5) - (1.57 * Utility.SafeConvertToDouble(comboSpoutDia.Text) + 3)) / 4) * 1.12), 0);
                int addCM = 20;
                if (_Type == 1)//Outer Added on 20-Apr-2015   as discussed with Dilen ji
                    addCM = 15;
                else
                {
                    addCM = 10;
                    if (DSHt != 0)
                        addCM = 5 + addCM;
                    if (FSHt != 0)
                        addCM = 5 + addCM;
                }
                #region  Upanel,Circular,4Panel and other type
                if (_BodyIndex1 == 0 || _BodyIndex1 == 1 || _BodyIndex1 == 3 || _BodyIndex1 == 4 ||
                    _BodyIndex1 == 5 || _BodyIndex1 == 7
                    || _BodyIndex1 == 9 || _BodyIndex1 == 10 || _BodyIndex1 == 11 || _BodyIndex1 == 12 || _BodyIndex1 == 13)
                {
                    #region Form Fit liner  and flenze liner
                    if (comboBoxlinertype1.SelectedIndex == 1 || comboBoxlinertype1.SelectedIndex == 2) //Form Fit liner  and flenze liner
                    {

                        if (comboBoxlinertype1.SelectedIndex == 2)
                            LinerFabricSize = _BagLenght + _BagWidth + 25;
                        else if (comboBoxlinertype1.SelectedIndex == 1)//Outer Added on 20-Apr-2015 as discussed with Dilen ji
                            LinerFabricSize = _BagLenght + _BagWidth + 5;

                        //if (_Type == 1)//Outer Added on 20-Apr-2015 as discussed with Dilen ji
                        //    LinerFabricSize = (_BagLenght - 4) + (_BagWidth - 4) + 5;


                        if (((_TopType == 0 || _TopType == 2 || _TopType == 5 || _TopType == 6) && _BottomType == 3)) //Top Skrit,Bottom Spout
                        {
                            if (_TopType == 0)
                                LinerCutLenght = ((_BagLenght + _BagWidth) / 2) - 10 + BottomSpoutDia + Height + 10 + DSHt + 5;
                            else
                                LinerCutLenght = SkritHt + 5 + BottomSpoutDia + Height + DSHt + addCM;
                        }
                        else
                        {
                            LinerCutLenght = FSHt + TopSpoutDia + BottomSpoutDia + Height + DSHt + addCM;
                            if (_TopType == 0 || _BottomType == 0)
                            {
                                LinerCutLenght += (((_BagLenght + _BagWidth) / 2) / 2) + 5;
                            }
                        }

                        //if (checkBoxlinerBuffle.Checked)// By Rikin 20-Apr-2015. Discussed with dilen ji 
                        //{ }
                    }
                    #endregion
                    #region Gazzeted Liner
                    else if (comboBoxlinertype1.SelectedIndex == 3) //Gazzeted Liner
                    {
                        #region Open Top, Flat Base
                        if ((_TopType == 0 && _BottomType == 0) || ((_TopType == 2 || _TopType == 5 || _TopType == 6) && _BottomType == 0)) //Open Top, Flat Base
                        {
                            if (_Type == 1)
                                LinerFabricSize = (_BagLenght - 4) + (_BagWidth - 4) + 5;
                            else
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = Bottom + Height + 80 + addCM;// as Per dilen ji by Rikin on 24-02-2015
                        }
                        #endregion
                        #region Top Skrit,Bottom Spout
                        else if ((_TopType == 2 || _TopType == 5 || _TopType == 6) && _BottomType == 3) //Top Skrit,Bottom Spout
                        {
                            LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = Bottom + Height + SkritHt + DSHt + addCM;// as Per dilen ji by Rikin on 24-02-2015

                        }
                        #endregion
                        #region Top Spout,Bottom Flat
                        else if (_TopType == 1 && _BottomType == 0) //Top Spout,Bottom Flat
                        {
                            LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = ((_BagWidth + _BagLenght) / 2) + Height + FSHt + addCM;

                        }
                        #endregion
                        #region Top Spout,Bottom Spout
                        else if (_TopType == 1 && _BottomType == 3) //Top Spout,Bottom Flat
                        {
                            LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = ((_BagWidth + _BagLenght) / 2) + Height + FSHt + DSHt + 20;

                        }
                        #endregion
                        #region Open Top,Bottom Spout
                        else if (_TopType == 0 && _BottomType == 3) //Open Top,Bottom Spout
                        {
                            LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = Bottom + Height + DSHt + 80 + 20;
                        }
                        #endregion
                    }
                    #endregion

                    else if (comboBoxlinertype1.SelectedIndex == 4)
                    {
                        LinerFabricSize = _BagLenght + _BagWidth + 5;
                        LinerCutLenght = _BagHeight + Convert.ToDouble(textSlitHt.Text) + _BagWidth + 10;
                    }

                    else if (comboBoxlinertype1.SelectedIndex == 5) // tray liner
                    {
                        if (comboType.SelectedIndex == 0) //internal
                            LinerFabricSize = (_BagLenght * 2) + (_BagWidth * 2) + 10; // LinerFabricSize = _BagLenght + _BagWidth + 8; 18.06.2021
                        else
                            LinerFabricSize = _BagLenght + _BagWidth;

                        LinerCutLenght = Convert.ToDouble(comboBoxlinerheight.Text) + (_BagWidth / 2) + 5;
                    }
                    LinerWt = LinerCutLenght * LinerFabricSize * 2 * Utility.SafeConvertToDouble(comboBoxlinermicron.Text) * Density;
                }
                #endregion
                #region Single Loop Bag Type
                else if (false) // Single Loop
                {

                    #region Open Top, Flat Base for Single Loop
                    if (_TopType == 0 && _BottomType == 0) //Open Top, Flat Base
                    {
                        if (comboBoxlinertype1.SelectedIndex == 0)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + 25;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + 25;
                            }
                        }


                    }
                    #endregion
                    #region Top Spout,Bottom Flat for Single Loop
                    if (_TopType == 1 && _BottomType == 0) //Top Spout,Bottom Flat
                    {
                        if (comboBoxlinertype1.SelectedIndex == 0)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + 25;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + 25;
                            }
                        }
                        else if (comboBoxlinertype1.SelectedIndex == 1)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5)
                                    + Utility.SafeConvertToDouble(comboSpoutHeight.Text) + TopSpoutDia + 2;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7) + Height
                                    + Utility.SafeConvertToDouble(comboSpoutHeight.Text) + TopSpoutDia + 2;

                            }
                        }
                        else if (comboBoxlinertype1.SelectedIndex == 2)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7) + (Height + 5)
                                    + Utility.SafeConvertToDouble(comboSpoutHeight.Text) + TopSpoutDia + 2;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7) + Height
                                    + Utility.SafeConvertToDouble(comboSpoutHeight.Text) + TopSpoutDia + 2;
                            }
                        }

                    }
                    #endregion
                    #region Top Spout,Bottom Spout for Single Loop
                    if (_TopType == 1 && _BottomType == 3) //Top Spout,Bottom Spout
                    {
                        if (comboBoxlinertype1.SelectedIndex == 0)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + 25;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + 25;
                            }
                        }
                        else if (comboBoxlinertype1.SelectedIndex == 1)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = DSHt + 5 + FSHt + 5 + BottomSpoutDia + 2 + (Height + 5)
                                    + TopSpoutDia + 2;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = DSHt + FSHt + BottomSpoutDia + 2 + (Height + 5)
                                    + TopSpoutDia + 2;
                            }
                        }
                    }
                    #endregion
                    #region Open Top,Bottom Spout for Single Loop
                    if (_TopType == 0 && _BottomType == 3) //Open Top,Bottom Spout
                    {
                        if (comboBoxlinertype1.SelectedIndex == 0)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + 25;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + 25;
                            }
                        }
                        else if (comboBoxlinertype1.SelectedIndex == 1)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5)
                                    + DSHt + BottomSpoutDia + 2;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + DSHt + BottomSpoutDia + 2;

                            }
                        }
                        else if (comboBoxlinertype1.SelectedIndex == 2)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7) + (Height + 5)
                                    + DSHt + BottomSpoutDia + 2;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7) + Height
                                    + DSHt + BottomSpoutDia + 2;
                            }
                        }
                    }
                    #endregion
                    LinerWt = LinerCutLenght * LinerFabricSize * 2 * Utility.SafeConvertToDouble(comboBoxlinermicron.Text) * Density;
                }
                #endregion
                #region Double Loop Bag Type
                else if (_BodyIndex1 == 6 || _BodyIndex1 == 8) //Double Loop
                {

                    #region Open Top, Flat Base for Double Loop
                    if (_TopType == 0 && _BottomType == 0) //Open Top, Flat Base
                    {
                        if (comboBoxlinertype1.SelectedIndex == 0)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + 25;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + 25;
                            }
                        }
                        else if (comboBoxlinertype1.SelectedIndex == 4)
                        {
                            LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = _BagHeight + Convert.ToDouble(textSlitHt.Text) + _BagWidth + 10;
                        }
                        LinerWt = LinerCutLenght * LinerFabricSize * 2 * Utility.SafeConvertToDouble(comboBoxlinermicron.Text) * Density;
                    }
                    #endregion
                    #region Top Skrit,Bottom Flat for Double Loop
                    if ((_TopType == 2 || _TopType == 5 || _TopType == 6) && _BottomType == 0) //Top Skrit,Bottom Flat
                    {
                        if (comboBoxlinertype1.SelectedIndex == 0)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + SkritHt + 15;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + SkritHt + 15;
                            }
                        }
                    }
                    #endregion
                    #region Top Skrit,Bottom Spout for Double Loop
                    if ((_TopType == 2 || _TopType == 5 || _TopType == 6) && _BottomType == 3) //Top Skrit,Bottom Spout
                    {
                        if (comboBoxlinertype1.SelectedIndex == 0)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + SkritHt + 15;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + SkritHt + 15;
                            }
                        }
                        else if (comboBoxlinertype1.SelectedIndex == 1)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + SkritHt + 15 + BottomSpoutDia + 2;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + SkritHt + 15 + BottomSpoutDia + 2;
                            }
                        }
                    }
                    #endregion
                    #region  Top Spout,Bottom Flat for Double Loop
                    if (_TopType == 1 && _BottomType == 0) //Top Spout,Bottom Flat
                    {
                        if (comboBoxlinertype1.SelectedIndex == 0)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + 15;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + 15;
                            }
                        }
                        else if (comboBoxlinertype1.SelectedIndex == 1)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + 15 + TopSpoutDia + 2;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + 15 + TopSpoutDia + 2;
                            }
                        }
                    }
                    #endregion
                    #region Top Spout,Bottom Spout for Double Loop
                    if (_TopType == 1 && _BottomType == 3) //Top Spout,Bottom Spout
                    {
                        if (comboBoxlinertype1.SelectedIndex == 0)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + 15;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + 15;
                            }
                        }
                        else if (comboBoxlinertype1.SelectedIndex == 1)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (FSHt + 5) + (DSHt + 5) + Height + 15 + TopSpoutDia + 2 + BottomSpoutDia + 2;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = FSHt + DSHt + Height + 15 + TopSpoutDia + 2 + BottomSpoutDia + 2;
                            }
                        }
                    }
                    #endregion
                    #region Open Top,Bottom Spout for Double Loop
                    if (_TopType == 0 && _BottomType == 3) //Open Top,Bottom Spout
                    {
                        if (comboBoxlinertype1.SelectedIndex == 0)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + 15;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + 15;
                            }
                        }
                        else if (comboBoxlinertype1.SelectedIndex == 1)
                        {
                            if (_Type == 0) //Internal
                            {
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                                LinerCutLenght = (Bottom * 2 + 7 + 2.5) + (Height + 5) + 15 + BottomSpoutDia + 2;
                            }
                            else //External
                            {
                                LinerFabricSize = _BagLenght + _BagWidth;
                                LinerCutLenght = (Bottom * 2 + 7) + Height + 15 + BottomSpoutDia + 2;
                            }
                        }
                    }
                    #endregion

                    #region Gazzeted Liner 11.Oct.2021
                    if (comboBoxlinertype1.SelectedIndex == 3) //Gazzeted Liner
                    {
                        #region Open Top, Flat Base
                        if ((_TopType == 0 && _BottomType == 0) || ((_TopType == 2 || _TopType == 5 || _TopType == 6) && _BottomType == 0)) //Open Top, Flat Base
                        {
                            if (_Type == 1)
                                LinerFabricSize = (_BagLenght - 4) + (_BagWidth - 4) + 5;
                            else
                                LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = Bottom + Height + 80 + addCM;// as Per dilen ji by Rikin on 24-02-2015
                        }
                        #endregion
                        #region Top Skrit,Bottom Spout
                        else if ((_TopType == 2 || _TopType == 5 || _TopType == 6) && _BottomType == 3) //Top Skrit,Bottom Spout
                        {
                            LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = Bottom + Height + SkritHt + DSHt + addCM;// as Per dilen ji by Rikin on 24-02-2015

                        }
                        #endregion
                        #region Top Spout,Bottom Flat
                        else if (_TopType == 1 && _BottomType == 0) //Top Spout,Bottom Flat
                        {
                            LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = ((_BagWidth + _BagLenght) / 2) + Height + FSHt + addCM;

                        }
                        #endregion
                        #region Top Spout,Bottom Spout
                        else if (_TopType == 1 && _BottomType == 3) //Top Spout,Bottom Flat
                        {
                            LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = ((_BagWidth + _BagLenght) / 2) + Height + FSHt + DSHt + 20;

                        }
                        #endregion
                        #region Open Top,Bottom Spout
                        else if (_TopType == 0 && _BottomType == 3) //Open Top,Bottom Spout
                        {
                            LinerFabricSize = _BagLenght + _BagWidth + 5;
                            LinerCutLenght = Bottom + Height + DSHt + 80 + 20;
                        }
                        #endregion
                        LinerWt = LinerCutLenght * LinerFabricSize * 2 * Utility.SafeConvertToDouble(comboBoxlinermicron.Text) * Density;
                    }
                    #endregion
                }
                #endregion
            }

        }
        private void SlitHtFormula()
        {
            SlitHt = (Math.Sqrt(((_BagLenght) * (_BagLenght)) + (_BagWidth) * (_BagWidth))) / 2;
        }
        private void TotalHtFormula()
        {
            if (Utility.SafeConvertToDouble(textSWL.Text) < 1250) TotalHt = Utility.SafeConvertToDouble(textFillHt.Text) + SlitHt + 10;
            else if (Utility.SafeConvertToDouble(textSWL.Text) >= 1250) TotalHt = Utility.SafeConvertToDouble(textFillHt.Text) + SlitHt + 10;
        }
        private void SpotCoverFormuala()
        {
            SpoutcoperWt = (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) / 2) + 12.5 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text);
        }

        private void LoopGRMTable()
        {
            if (textSWL.Text == "")
                textSWL.Text = "0";
            //if (comboLoopGrm.SelectedIndex == 1)
            //{
            if (_BodyIndex1 == 0 && comboBody2.SelectedIndex == 4) //UPanel + Builder Bag
            {
                if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 750)
                    comboLoopGrm.Text = "22";
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 750)
                    comboLoopGrm.Text = "27";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 750)
                    comboLoopGrm.Text = "37";

                if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                    comboLoopGrm.Text = "30";
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                    comboLoopGrm.Text = "35";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                    comboLoopGrm.Text = "45";

                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //5:1
                    comboLoopGrm.Text = "40";
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //6:1
                    comboLoopGrm.Text = "45";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //8:1
                    comboLoopGrm.Text = "55";

                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //5:1
                    comboLoopGrm.Text = "50";
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //6:1
                    comboLoopGrm.Text = "55";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //8:1
                    comboLoopGrm.Text = "65";

                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1750) //5:1
                    comboLoopGrm.Text = "55";
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1750) //6:1
                    comboLoopGrm.Text = "65";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1750) //8:1
                    comboLoopGrm.Text = "75";

                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //5:1
                    comboLoopGrm.Text = "65";
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //6:1
                    comboLoopGrm.Text = "75";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //8:1
                    comboLoopGrm.Text = "85";
            }

                //_BodyIndex1 == 2 is added by Rikin on 25-Feb-2015 
            // According to anjul bhai Buffle bag calculation is same as Upenal bag
            else if (_BodyIndex1 == 0 || _BodyIndex1 == 2 || _BodyIndex1 == 3 ||
                   _BodyIndex1 == 4 || _BodyIndex1 == 7 ||
                 _BodyIndex1 == 8 || _BodyIndex1 == 9 || _BodyIndex1 == 10)
            {

                if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 750)
                    comboLoopGrm.Text = "25";
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 750)
                    comboLoopGrm.Text = "25";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 750)
                    comboLoopGrm.Text = "35";


                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                {

                    comboLoopGrm.Text = "35"; //Only Single Change
                    textShortLeg.Text = "50";
                }
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                {
                    comboLoopGrm.Text = "45";
                    textShortLeg.Text = "50";
                }
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                {
                    comboLoopGrm.Text = "45";
                    textShortLeg.Text = "50";
                }

                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //5:1
                {
                    textShortLeg.Text = "50";
                    comboLoopGrm.Text = "45";
                }
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //6:1
                {
                    comboLoopGrm.Text = "45";
                    textShortLeg.Text = "50";
                }
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //8:1
                { comboLoopGrm.Text = "55"; textShortLeg.Text = "50"; }

                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //5:1
                { comboLoopGrm.Text = "55"; textShortLeg.Text = "50"; }
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //6:1
                { comboLoopGrm.Text = "55"; textShortLeg.Text = "60"; }
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //8:1
                    comboLoopGrm.Text = "65";

                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1750) //5:1
                    comboLoopGrm.Text = "55";
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1750) //6:1
                    comboLoopGrm.Text = "65";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1750) //8:1
                    comboLoopGrm.Text = "75";

                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //5:1
                { comboLoopGrm.Text = "65"; textShortLeg.Text = "60"; }
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //6:1
                { comboLoopGrm.Text = "75"; textShortLeg.Text = "60"; }
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //8:1
                { comboLoopGrm.Text = "85"; textShortLeg.Text = "60"; }
            }
            else if (_BodyIndex1 == 1) //Circular
            {

                if ((comboSF.SelectedIndex == 0 || comboSF.SelectedIndex == 1 || comboSF.SelectedIndex == 2) && Utility.SafeConvertToDouble(textSWL.Text) <= 500)
                {
                    comboLoopGrm.Text = "45";
                    comboLoopL.Text = "30";
                }
                //else if ( && Utility.SafeConvertToDouble (textSWL.Text) <= 500)
                //    comboLoopGrm.Text = "45";
                else if ((comboSF.SelectedIndex == 0 || comboSF.SelectedIndex == 1) && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)//5:1
                {
                    comboLoopGrm.Text = "45";
                    comboLoopL.Text = "30";
                }
                else if ((comboSF.SelectedIndex == 0) && Utility.SafeConvertToDouble(textSWL.Text) <= 1250)//5:1//6:1
                {
                    comboLoopGrm.Text = "45";
                    comboLoopL.Text = "35";
                }

                else if (((comboSF.SelectedIndex == 1) && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) ||
                 (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500)) //5:1//6:1
                {
                    comboLoopGrm.Text = "55";
                    comboLoopL.Text = "40";
                }
                else if (((comboSF.SelectedIndex == 1) && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) || (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1750))  //6:1
                {
                    comboLoopGrm.Text = "60";
                    comboLoopL.Text = "45";
                }
                //All most above conditions are enogugh. by Rikin on 25-Feb-2015

                //else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble (textSWL.Text) <= 1000)
                //    comboLoopGrm.Text = "40";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                    comboLoopGrm.Text = "55";



                //else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble (textSWL.Text) <= 1250) //5:1
                //    comboLoopGrm.Text = "40";
                //else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble (textSWL.Text) <= 1250) //6:1
                //    comboLoopGrm.Text = "50";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //8:1
                    comboLoopGrm.Text = "65";

                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //5:1
                {
                    comboLoopGrm.Text = "55";
                    comboLoopL.Text = "45";
                }
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //6:1
                    comboLoopGrm.Text = "65";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //8:1
                    comboLoopGrm.Text = "75";


                else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //5:1
                    comboLoopGrm.Text = "65";
                else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //6:1
                    comboLoopGrm.Text = "75";
                else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //8:1
                    comboLoopGrm.Text = "85";


            }

                //Moved in above calculation

            else if (_BodyIndex1 == 2) //Buffle
            {

                if (comboBuffleType.SelectedIndex == 3 || comboBuffleType.SelectedIndex == 5)  //CrossCorner
                {
                    if ((comboSF.SelectedIndex == 0 || comboSF.SelectedIndex == 1) && Utility.SafeConvertToDouble(textSWL.Text) <= 500)
                        comboLoopGrm.Text = "35";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 500)
                        comboLoopGrm.Text = "40";

                    if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        comboLoopGrm.Text = "40";
                    else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        comboLoopGrm.Text = "40";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        comboLoopGrm.Text = "50";



                    else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //5:1
                        comboLoopGrm.Text = "40";
                    else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //6:1
                        comboLoopGrm.Text = "50";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //8:1
                        comboLoopGrm.Text = "60";

                    else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //5:1
                        comboLoopGrm.Text = "50";
                    else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //6:1
                        comboLoopGrm.Text = "60";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //8:1
                        comboLoopGrm.Text = "70";


                    else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //5:1
                        comboLoopGrm.Text = "60";
                    else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //6:1
                        comboLoopGrm.Text = "70";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //8:1
                        comboLoopGrm.Text = "80";
                }
                else
                {
                    if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 750)
                        comboLoopGrm.Text = "20";
                    else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 750)
                        comboLoopGrm.Text = "25";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 750)
                        comboLoopGrm.Text = "35";


                    else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        comboLoopGrm.Text = "30"; //Only Single Change
                    else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        comboLoopGrm.Text = "35";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        comboLoopGrm.Text = "45";

                    else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //5:1
                        comboLoopGrm.Text = "35";
                    else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //6:1
                        comboLoopGrm.Text = "45";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250) //8:1
                        comboLoopGrm.Text = "55";

                    else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //5:1
                        comboLoopGrm.Text = "45";
                    else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //6:1
                        comboLoopGrm.Text = "55";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1500) //8:1
                        comboLoopGrm.Text = "65";

                    else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 1750) //5:1
                        comboLoopGrm.Text = "55";
                    else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 1750) //6:1
                        comboLoopGrm.Text = "65";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 1750) //8:1
                        comboLoopGrm.Text = "75";

                    else if (comboSF.SelectedIndex == 0 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //5:1
                        comboLoopGrm.Text = "65";
                    else if (comboSF.SelectedIndex == 1 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //6:1
                        comboLoopGrm.Text = "75";
                    else if (comboSF.SelectedIndex == 2 && Utility.SafeConvertToDouble(textSWL.Text) <= 2000) //8:1
                        comboLoopGrm.Text = "85";
                }
            }
            // }
        }
        private void LoopWtFormula()
        {

            double SWL = Utility.SafeConvertToDouble(textSWL.Text);
            #region Upanel + Wider Fold , Upanel + UN ,Check Loop Till Bottom is selected
            //Upanel + Wider Fold , Upanel + UN ,Check Loop Till Bottom is selected
            //(_BodyIndex1 == 0 && comboBody2.SelectedIndex == 3) || wider bottom was 
            //(_BodyIndex1 == 0 && comboBody3.SelectedIndex == 1) it is not required. we can select till the bottom option instade
            if (((_BodyIndex1 == 0 || _BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4 || _BodyIndex1 == 7 ||
                _BodyIndex1 == 8 || _BodyIndex1 == 9 || _BodyIndex1 == 10)
               && checkLoopTillBottom.Checked))
            {
                if (comboSF.SelectedIndex == 0) // 5:1
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    //LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .80 * _BagHeight); //Changed by Rikin on 11-mar-2015 as per dilen ji

                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                    {
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + (_BagHeight - Utility.SafeConvertToDouble(textLoopDropLenght.Text) - 5));
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    }
                    else
                    { 
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + (_BagHeight - 5)); }
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 100)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 100)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);
                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }

                else if (comboSF.SelectedIndex == 1 || comboSF.SelectedIndex == 2) //6:1,8:1
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    //LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text)) * 2 + 60 + .80 * _BagHeight); //Changed by Rikin on 11-mar-2015 as per dilen ji

                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                    {
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text)) * 2 + 60 + (_BagHeight - Utility.SafeConvertToDouble(textLoopDropLenght.Text) - 5));
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    }
                    else
                    {
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text)) * 2 + 60 + (_BagHeight - 5));
                    }
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 100)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 100)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }

            }
            #endregion

            #region UPanel + Builder Bag
            //UPanel + Builder Bag  

            else if (_BodyIndex1 == 0 && (comboBody2.SelectedIndex == 4)) //UPanel + Builder Bag /Tunal (Tunal is added by Rikin) //|| comboBody2.SelectedIndex == 5
            {
                if ((comboSF.SelectedIndex == 0 || comboSF.SelectedIndex == 1 || comboSF.SelectedIndex == 2) && SWL <= 1000) // SF =5:1,SWL <= 1000
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 40 + .66 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 90)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 90)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }

                else if ((comboSF.SelectedIndex == 0 || comboSF.SelectedIndex == 1) && SWL <= 1500) // SF =5:1,SWL <= 1500
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 90)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 90)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);
                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }

                else if (comboSF.SelectedIndex == 2 && SWL <= 1500) // SF =8:1,SWL <= 1500
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 60 + .80 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 90)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 90)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }


                else if ((comboSF.SelectedIndex == 0 || comboSF.SelectedIndex == 1 || comboSF.SelectedIndex == 2) && SWL <= 2000) // SF =5:1,SWL <= 2000
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 60 + .80 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 90)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 90)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }

            }
            #endregion
            #region Cross Corner
            else if ((_BodyIndex1 == 1 && comboLoopConst.SelectedIndex == 2)
                    || (comboBody2.SelectedIndex == 2 && (comboBuffleType.SelectedIndex == 3
                    || comboBuffleType.SelectedIndex == 5 || comboBuffleType.SelectedIndex == 5)) || (comboLoopConst.SelectedIndex == 2)) //Cross Corner
            {
                if (comboSF.SelectedIndex == 0) // 5:1
                {
                    if (SWL <= 1249)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text); //CHANGES BY SHARAD ON 10TH aUGUST FROM 7
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 70; //changes by anjul on dated 20th July 2017
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 1250)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 70;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 1500)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 80;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 2000)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 90;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                }
                else if (comboSF.SelectedIndex == 1 || comboSF.SelectedIndex == 2) //6:1,8:1
                {
                    if (SWL <= 1000)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text); ;
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 60;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else
                        if (SWL <= 1299)
                        {
                            LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                            LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 80;
                            if (textLoopLenght.Text != "")
                                LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                            if (checkBoxDropLoop.Checked)
                                LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                            LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                        }
                        else if (SWL <= 1300)
                        {
                            LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                            LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 90;
                            if (textLoopLenght.Text != "")
                                LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                            if (checkBoxDropLoop.Checked)
                                LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                            LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                        }
                        else if (SWL <= 2000)
                        {
                            LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                            LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 100;
                            if (textLoopLenght.Text != "")
                                LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                            if (checkBoxDropLoop.Checked)
                                LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                            LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                        }
                }
            }
            #endregion
            else if (_BodyIndex1 == 0 || _BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4 || _BodyIndex1 == 7 || _BodyIndex1 == 8 || _BodyIndex1 == 11)
            {
                if (comboSF.SelectedIndex == 0) // 5:1
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .70 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 100)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 100)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin  
                }
                else if (comboSF.SelectedIndex == 1) //6:1
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text)) * 2 + 50 + .75 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 100)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 100)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }
                else if (comboSF.SelectedIndex == 2) //8:1
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 60 + .80 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 100)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 100)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }
            }

            #region Corner
            else if (comboLoopConst.SelectedIndex == 1) // Corner
            {
                if ((comboSF.SelectedIndex == 0 || comboSF.SelectedIndex == 1 || comboSF.SelectedIndex == 2) && SWL <= 1000) // SF =5:1,SWL <= 1000
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 40 + .66 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 95)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 95)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }

                else if ((comboSF.SelectedIndex == 0 || comboSF.SelectedIndex == 1) && SWL <= 1500) // SF =5:1,SWL <= 1500
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 95)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 95)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }

                else if (comboSF.SelectedIndex == 2 && SWL <= 1500) // SF =8:1,SWL <= 1500
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 60 + .80 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 95)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 95)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }


                else if ((comboSF.SelectedIndex == 0 || comboSF.SelectedIndex == 1 || comboSF.SelectedIndex == 2) && SWL <= 2000) // SF =5:1,SWL <= 2000
                {
                    LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                    LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 60 + .80 * _BagHeight);
                    if (textLoopLenght.Text != "")
                        LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    if (checkBoxDropLoop.Checked)
                        LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) <= 95)
                        LoopCutLenght = 160;
                    if (checkBoxTunnel.Checked && Convert.ToInt32(textBodyH.Text) > 95)
                        LoopCutLenght = ((Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50 + .75 * _BagHeight);

                    LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }
            }
            #endregion


            else if (_BodyIndex1 == 2 || _BodyIndex1 == 1)
            {
                if (comboSF.SelectedIndex == 0) // 5:1
                {
                    if (SWL <= 750)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 1000)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 60;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 1250)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 70;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 1500)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 80;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 2000)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 90;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                }
                else if (comboSF.SelectedIndex == 1 || comboSF.SelectedIndex == 2) //6:1
                {
                    if (SWL <= 750)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 50;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 1000)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 70;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 1250)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 80;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 1500)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 90;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                    else if (SWL <= 2000)
                    {
                        LoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);
                        LoopCutLenght = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2) + 90;
                        if (textLoopLenght.Text != "")
                            LoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                        if (checkBoxDropLoop.Checked)
                            LoopCutLenght = LoopCutLenght + (Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2);
                        LoopWt = LoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                    }
                }
            }

            LoopFabricSize = LoopFabricSize * 10;
            //}

            #region Loop Constant is (Full Loop + Cross Corner)
            // Loop Constant is (Full Loop + Cross Corner)
            if (comboLoopConst.SelectedIndex == 3 && (_BodyIndex1 == 0 || _BodyIndex1 == 1
                           || _BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4))
            {
                FullLoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text);

                if (SWL <= 1000)
                {
                    FullLoopCutLenght = (_BagHeight * 2) + _BagWidth - 30;
                    if (textLoopLenght.Text != "")
                        FullLoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    FullLoopWt = FullLoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }
                else if (SWL <= 2000)
                {
                    FullLoopCutLenght = (_BagHeight * 2) + _BagWidth - 40;
                    if (textLoopLenght.Text != "")
                        FullLoopCutLenght = Utility.SafeConvertToDouble(textLoopLenght.Text);
                    FullLoopWt = FullLoopCutLenght * Utility.SafeConvertToDouble(comboLoopGrm.Text) * Utility.SafeConvertToDouble(textLoopNo.Text);// changed from 4 to value of text box Rikin 
                }
            }
            #endregion
        }


        private void FSWtFormula()   //Fill Spout, Top Spout
        {
            if (comboTopType.SelectedIndex == 1 || comboTopType.SelectedIndex == 3 || comboTopType.SelectedIndex == 4) //Top Spout,conical Top
            {
                if (comboSpoutType.SelectedIndex == 4) //Iris/Pyjama Closure
                {
                    double iris = (Utility.SafeConvertToDouble(comboSpoutDia.Text) / 2) + 12.5 + Utility.SafeConvertToDouble(comboSpoutHeight.Text);

                    if (checkBoxDoubleFoldTop.Checked) //18.08.2021 add Double Fold
                    {
                        if (comboBody3.SelectedIndex == 2)
                            FSFabricSize = iris + 18;
                        else
                            FSFabricSize = iris;
                        FSCutLenght = (3.14 * (Utility.SafeConvertToDouble(comboSpoutDia.Text) + 18));
                    }
                    else
                    {
                        if (comboBody3.SelectedIndex == 2) //FDA
                        {
                            iris = iris + 5; //05.02.2022 Correction made as check with Mahesh
                            FSFabricSize = iris + 3;
                        }
                        else
                        {
                            if (checkTopEdgeHemming.Checked)
                                FSFabricSize = iris + 4;
                            else
                                FSFabricSize = iris;
                        }
                        FSCutLenght = (3.14 * (Utility.SafeConvertToDouble(comboSpoutDia.Text) + 4));
                    }
                    FSWt = (FSCutLenght * iris * (Utility.SafeConvertToDouble(comboSpoutGSM.Text) + Utility.SafeConvertToDouble(comboSpoutLamiGSM.Text)))
                        * Utility.SafeConvertToDouble(textFSNo.Text);   // + 160000;
                }

                else if (comboSpoutType.SelectedIndex == 2) //Petal Closure,Simple
                {
                    if (checkBoxDoubleFoldTop.Checked) //20.08.2021 add Double Fold
                    {
                        if (checkBoxSpoutLam.Checked)
                            FSFabricSize = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 18);
                        else
                            FSFabricSize = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 18);
                        FSCutLenght = 3.14 * (Utility.SafeConvertToDouble(comboSpoutDia.Text) + 18);

                    }
                    else
                    {
                        if (checkBoxSpoutLam.Checked)
                        {
                            if (checkTopEdgeHemming.Checked)
                                FSFabricSize = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 8);
                            else
                                FSFabricSize = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 5);
                        }
                        else
                        {
                            FSFabricSize = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 12);
                        }
                        FSCutLenght = 3.14 * (Utility.SafeConvertToDouble(comboSpoutDia.Text) + 4);
                    }
                    FSWt = FSCutLenght * FSFabricSize * (Utility.SafeConvertToDouble(comboSpoutGSM.Text) + Utility.SafeConvertToDouble(comboSpoutLamiGSM.Text))
                        * Utility.SafeConvertToDouble(textFSNo.Text);
                }
                else if (comboSpoutType.SelectedIndex == 8) //Tube
                {
                    if (checkBoxDoubleFoldTop.Checked) //20.08.2021 add Double Fold
                    {

                        if (comboBody3.SelectedIndex == 2 || checkBoxSpoutLam.Checked == false)
                            FSCutLenght = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 18);
                        else
                            FSCutLenght = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 12);
                        FSFabricSize = (1.57 * Utility.SafeConvertToDouble(comboSpoutDia.Text)) + 8;
                        FSFabricSize = Math.Round(FSFabricSize, 0);
                    }
                    else
                    {
                        if (comboBody3.SelectedIndex == 2 || checkBoxSpoutLam.Checked == false)
                            FSCutLenght = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 10);
                        else
                        {
                            if (checkTopEdgeHemming.Checked)
                                FSCutLenght = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 8);
                            else
                                FSCutLenght = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 5);
                        }
                        if (checkTopEdgeHemming.Checked)
                            FSFabricSize = (1.57 * Utility.SafeConvertToDouble(comboSpoutDia.Text)) + 1;
                        else
                            FSFabricSize = (1.57 * Utility.SafeConvertToDouble(comboSpoutDia.Text)) + 1;
                        FSFabricSize = Math.Round(FSFabricSize, 0);
                    }
                    FSWt = FSCutLenght * FSFabricSize * (Utility.SafeConvertToDouble(comboSpoutGSM.Text) + Utility.SafeConvertToDouble(comboSpoutLamiGSM.Text))
                        * Utility.SafeConvertToDouble(textFSNo.Text) * 2;
                }
                else if (comboSpoutType.SelectedIndex == 9) //Edge hemming added on 19th July 2022 by manish
                {
                    if (checkBoxDoubleFoldTop.Checked) //20.08.2021 add Double Fold
                    {

                        FSFabricSize = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 13);
                        FSCutLenght = (3.14 * (Utility.SafeConvertToDouble(comboSpoutDia.Text) + 10));
                        FSFabricSize = Math.Round(FSFabricSize, 0);
                    }
                    else
                    {
                        FSFabricSize = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 8);
                        FSCutLenght = (3.14 * (Utility.SafeConvertToDouble(comboSpoutDia.Text) + 4));
                        FSFabricSize = Math.Round(FSFabricSize, 0);
                    }
                    FSWt = FSCutLenght * FSFabricSize * (Utility.SafeConvertToDouble(comboSpoutGSM.Text) + Utility.SafeConvertToDouble(comboSpoutLamiGSM.Text))
                        * Utility.SafeConvertToDouble(textFSNo.Text) * 2;
                }
                else
                {
                    if (checkBoxDoubleFoldTop.Checked) //20.08.2021 add Double Fold
                    {

                        if (comboBody3.SelectedIndex == 2)
                            FSFabricSize = Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 18;
                        else
                        {
                            if (checkBoxSpoutLam.Checked)
                                FSFabricSize = Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 12;
                            else
                                FSFabricSize = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 18);
                        }
                        FSCutLenght = 3.14 * (Utility.SafeConvertToDouble(comboSpoutDia.Text) + 11);
                    }
                    else
                    {
                        if (comboBody3.SelectedIndex == 2)
                            FSFabricSize = Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 10;
                        else
                        {
                            if (checkBoxSpoutLam.Checked)
                            {
                                if (checkTopEdgeHemming.Checked)
                                    FSFabricSize = Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 8;
                                else
                                    FSFabricSize = Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 5;
                            }
                            else
                                FSFabricSize = (Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 12);
                        }
                        FSCutLenght = 3.14 * (Utility.SafeConvertToDouble(comboSpoutDia.Text) + 4);
                    }
                    FSWt = FSCutLenght * FSFabricSize * (Utility.SafeConvertToDouble(comboSpoutGSM.Text) + Utility.SafeConvertToDouble(comboSpoutLamiGSM.Text)) * Utility.SafeConvertToDouble(textFSNo.Text); //+ 70000(Tie Wt) Remove by suggestion of Sabir on 4august 2008;
                }
            }

        }

        private void TopFlapWtFormula()
        {
            if (checktopflap.Checked)
            {
                if (textBoxtopflapnosflap.Text == "")
                    textBoxtopflapnosflap.Text = "0";
                if (_Type == 0)
                {
                    TopFlapFabricSize = _BagLenght + 5;
                    if (textTopFlapFabricsize.Text != "")
                        TopFlapFabricSize = Utility.SafeConvertToDouble(textTopFlapFabricsize.Text);

                    TopFlapCutLenght = _BagWidth + 15;
                    if (textTopFlapCutlenght.Text != "")
                        TopFlapCutLenght = Utility.SafeConvertToDouble(textTopFlapCutlenght.Text);
                    TopFlapWt = TopFlapCutLenght * TopFlapFabricSize * (Utility.SafeConvertToDouble(comboBoxtopflapgsm.Text)
                        + Utility.SafeConvertToDouble(comboTopflapLamiGsm.Text)) * Utility.SafeConvertToDouble(textBoxtopflapnosflap.Text);
                }
                else
                {
                    TopFlapFabricSize = _BagLenght - 5;
                    if (textTopFlapFabricsize.Text != "")
                        TopFlapFabricSize = Utility.SafeConvertToDouble(textTopFlapFabricsize.Text);

                    TopFlapCutLenght = _BagWidth + 10;
                    if (textTopFlapCutlenght.Text != "")
                        TopFlapCutLenght = Utility.SafeConvertToDouble(textTopFlapCutlenght.Text);
                    TopFlapWt = TopFlapCutLenght * TopFlapFabricSize * (Utility.SafeConvertToDouble(comboBoxtopflapgsm.Text)
                        + Utility.SafeConvertToDouble(comboTopflapLamiGsm.Text)) * Utility.SafeConvertToDouble(textBoxtopflapnosflap.Text);
                }
            }
        }

        private void BottomFlapWtFormula()
        {
            // txtBottomFlap.Text is added by Rikin on 12-Feb-2015
            if (checkBottomflap.Checked)
            {
                if (_Type == 0)
                {
                    BottomFlapFabricSize = _BagWidth + 5;
                    BottomFlapCutLenght = _BagLenght + 15;
                    if (textTopFlapCutlenght.Text != "")
                        BottomFlapCutLenght = Utility.SafeConvertToDouble(textBottomFlapCutLenght.Text);
                    BottomFlapWt = (BottomFlapCutLenght * BottomFlapFabricSize * (Utility.SafeConvertToDouble(comboBottomflapGSM.Text)
                        + Utility.SafeConvertToDouble(comboBottomflapLamiGSM.Text))) * (Utility.SafeConvertToDouble(txtBottomFlap.Text));
                }
                else
                {
                    BottomFlapFabricSize = _BagWidth - 5;
                    BottomFlapCutLenght = _BagLenght + 10;
                    if (textTopFlapCutlenght.Text != "")
                        BottomFlapCutLenght = Utility.SafeConvertToDouble(textBottomFlapCutLenght.Text);
                    BottomFlapWt = (BottomFlapCutLenght * BottomFlapFabricSize * (Utility.SafeConvertToDouble(comboBottomflapGSM.Text)
                        + Utility.SafeConvertToDouble(comboBottomflapLamiGSM.Text))) * (Utility.SafeConvertToDouble(txtBottomFlap.Text));
                }
            }

        }


        private void DSWtFormula()  // D/S
        {
            if (comboBoxbottomtype.SelectedIndex == 3) //Bottom Spout
            {

                if (checkBoxDoubleFoldBottom.Checked) //20.08.2021 add Double Fold
                {
                    if (checkFillerCord.Checked)
                        DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 18);
                    else
                        DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 12);
                    DSCutLenght = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) + 11);
                }
                else
                {
                    if (checkFillerCord.Checked)
                        DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 10);
                    else
                    {
                        if (checkBottomEdgeHemming.Checked)
                            DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 8);
                        else
                            DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 5);
                    }
                    DSCutLenght = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) + 4);
                }


                DSWt = DSCutLenght * DSFabricSize * (Utility.SafeConvertToDouble(comboBoxbottomgsm1.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM.Text))
                    * Utility.SafeConvertToDouble(textDSNo.Text); //+ 70000(Tie Wt) Remove by suggestion of Sabir on 4august 2008;
            }

            if (comboBoxbottomsubtype.SelectedIndex == 6) //tube
            {
                if (checkBoxDoubleFoldBottom.Checked) //20.08.2021 add Double Fold
                {
                    if (comboBody3.SelectedIndex == 2 || checkBoxbottomlam1.Checked == false)
                        DSCutLenght = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 18);
                    else
                        DSCutLenght = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 12);
                    DSFabricSize = (1.57 * Utility.SafeConvertToDouble(comboBoxbottomdia.Text)) + 8;
                    DSFabricSize = Math.Round(DSFabricSize, 0);
                }
                else
                {
                    if (comboBody3.SelectedIndex == 2 || checkBoxbottomlam1.Checked == false)
                        DSCutLenght = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 10);
                    else
                        DSCutLenght = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 5);
                    DSFabricSize = (1.57 * Utility.SafeConvertToDouble(comboBoxbottomdia.Text)) + 1;
                    DSFabricSize = Math.Round(DSFabricSize, 0);
                }
                DSWt = DSCutLenght * DSFabricSize * (Utility.SafeConvertToDouble(comboBoxbottomgsm1.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM.Text))
                    * Utility.SafeConvertToDouble(textDSNo.Text) * 2;

            }

            if (comboBoxbottomsubtype.SelectedIndex == 2) //Iris Closure
            {
                double iris = (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) / 2) + 12.5 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text);
                if (checkBoxDoubleFoldBottom.Checked) //20.08.2021 add Double Fold
                {
                    if (comboBody3.SelectedIndex == 2) //FDA
                        DSFabricSize = iris + 12;
                    else
                        DSFabricSize = iris + 18;
                    DSCutLenght = (3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) + 12));
                }
                else
                {
                    if (comboBody3.SelectedIndex == 2) //FDA
                        DSFabricSize = iris + 5;
                    else if (checkBottomEdgeHemming.Checked)
                        DSFabricSize = iris + 4;
                    else
                        DSFabricSize = iris;
                    DSCutLenght = (3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) + 4));
                }
                DSWt = (DSCutLenght * iris * (Utility.SafeConvertToDouble(comboBoxbottomgsm1.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM.Text)))
                    * Utility.SafeConvertToDouble(textDSNo.Text);   // + 160000;
            }

            if (comboBoxbottomsubtype.SelectedIndex == 1 || comboBoxbottomsubtype.SelectedIndex == 0) //Petal Closure,Simple
            {
                if (checkBoxDoubleFoldBottom.Checked) //20.08.2021 add Double Fold
                {
                    if (comboBody3.SelectedIndex == 2)
                        DSFabricSize = Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 20;
                    else
                    {
                        if (checkFillerCord.Checked && comboBoxbottomtype.SelectedIndex == 3)
                            DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 17);
                        else
                            DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 12);
                    }
                    DSCutLenght = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) + 11);
                }
                else
                {
                    if (comboBody3.SelectedIndex == 2)
                        DSFabricSize = Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 13;
                    else
                    {
                        if (checkFillerCord.Checked && comboBoxbottomtype.SelectedIndex == 3)
                            DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 10);
                        else if (checkBottomEdgeHemming.Checked)
                            DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 8);
                        else
                            DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 5);
                    }
                    //DSFabricSize = (Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 5);

                    DSCutLenght = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) + 4);
                }
                DSWt = DSCutLenght * DSFabricSize * (Utility.SafeConvertToDouble(comboBoxbottomgsm1.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM.Text))
                    * Utility.SafeConvertToDouble(textDSNo.Text);
            }
            if (_BodyIndex1 == 9) // Conical bag Three Piece
            {
                double OneSideDia = (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) * 3.14) / 4;
                if (checkBoxDoubleFoldBottom.Checked) //20.08.2021 add Double Fold
                {
                    // Body Spout Formula
                    DSCutLenght = (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) + 11) * 3.14;
                    DSFabricSize = Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 12;
                }
                else
                {
                    // Body Spout Formula
                    DSCutLenght = (Utility.SafeConvertToDouble(comboBoxbottomdia.Text) + 4) * 3.14;
                    DSFabricSize = Utility.SafeConvertToDouble(comboBoxbottomheight.Text) + 5;
                }
                DSWt = DSCutLenght * DSFabricSize * (Utility.SafeConvertToDouble(comboBoxbottomgsm1.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM.Text))
                * Utility.SafeConvertToDouble(textDSNo.Text);
            }
        }

        private void DSWtFormula1()  // D/S
        {
            if (comboBoxbottomtype1.SelectedIndex == 3) //Bottom Spout
            {
                if (checkBoxDoubleFoldBottomSpout.Checked) //20.08.2021 add Double Fold
                {
                    if (checkFillerCord.Checked)
                        DSFabricSize1 = (Utility.SafeConvertToDouble(comboBoxbottomheight1.Text) + 17);
                    else
                        DSFabricSize1 = (Utility.SafeConvertToDouble(comboBoxbottomheight1.Text) + 12);
                    DSCutLenght1 = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) + 11);
                }
                else
                {
                    if (checkFillerCord.Checked)
                        DSFabricSize1 = (Utility.SafeConvertToDouble(comboBoxbottomheight1.Text) + 10);
                    else
                        DSFabricSize1 = (Utility.SafeConvertToDouble(comboBoxbottomheight1.Text) + 5);
                    DSCutLenght1 = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) + 4);
                }
                DSWt1 = DSCutLenght1 * DSFabricSize1 * (Utility.SafeConvertToDouble(comboBoxbottomgsm3.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM1.Text))
                    * Utility.SafeConvertToDouble(textDSNo1.Text); //+ 70000(Tie Wt) Remove by suggestion of Sabir on 4august 2008;
            }

            if (comboBoxbottomsubtype1.SelectedIndex == 2) //Iris Closure
            {
                double iris = 0;// (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) / 2) + 12.5 + Utility.SafeConvertToDouble(comboBoxbottomheight1.Text);
                if (checkBoxDoubleFoldBottomSpout.Checked) //20.08.2021 add Double Fold
                {
                    iris = (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) / 2) + 18 + Utility.SafeConvertToDouble(comboBoxbottomheight1.Text);
                    DSFabricSize1 = iris;
                    DSCutLenght1 = (3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) + 11));
                }
                else
                {
                    iris = (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) / 2) + 12.5 + Utility.SafeConvertToDouble(comboBoxbottomheight1.Text);
                    DSFabricSize1 = iris;
                    DSCutLenght1 = (3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) + 4));
                }
                DSWt1 = (DSCutLenght1 * iris * (Utility.SafeConvertToDouble(comboBoxbottomgsm3.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM1.Text)))
                    * Utility.SafeConvertToDouble(textDSNo1.Text);   // + 160000;
            }

            if (comboBoxbottomsubtype1.SelectedIndex == 1 || comboBoxbottomsubtype1.SelectedIndex == 0) //Petal Closure,Simple
            {
                if (checkBoxDoubleFoldBottomSpout.Checked) //20.08.2021 add Double Fold
                {
                    if (checkFillerCord.Checked && comboBoxbottomtype1.SelectedIndex == 3)
                        DSFabricSize1 = (Utility.SafeConvertToDouble(comboBoxbottomheight1.Text) + 18);
                    else
                        DSFabricSize1 = (Utility.SafeConvertToDouble(comboBoxbottomheight1.Text) + 12);

                    DSCutLenght1 = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) + 11);
                }
                else
                {

                    if (checkFillerCord.Checked && comboBoxbottomtype1.SelectedIndex == 3)
                        DSFabricSize1 = (Utility.SafeConvertToDouble(comboBoxbottomheight1.Text) + 10);
                    else
                        DSFabricSize1 = (Utility.SafeConvertToDouble(comboBoxbottomheight1.Text) + 5);

                    DSCutLenght1 = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) + 4);
                }
                DSWt1 = DSCutLenght1 * DSFabricSize1 * (Utility.SafeConvertToDouble(comboBoxbottomgsm3.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM1.Text))
                    * Utility.SafeConvertToDouble(textDSNo1.Text);
            }
            if (_BodyIndex1 == 9) // Conical bag Three Piece
            {
                double OneSideDia = (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) * 3.14) / 4;
                if (checkBoxDoubleFoldBottomSpout.Checked) //20.08.2021 add Double Fold
                {
                    // Body Spout Formula
                    DSCutLenght1 = (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) + 11) * 3.14;
                    DSFabricSize1 = Utility.SafeConvertToDouble(comboBoxbottomheight1.Text) + 12;
                }
                else
                {
                    // Body Spout Formula
                    DSCutLenght1 = (Utility.SafeConvertToDouble(comboBoxbottomdia1.Text) + 4) * 3.14;
                    DSFabricSize1 = Utility.SafeConvertToDouble(comboBoxbottomheight1.Text) + 5;
                }
                DSWt1 = DSCutLenght1 * DSFabricSize1 * (Utility.SafeConvertToDouble(comboBoxbottomgsm3.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM1.Text))
                * Utility.SafeConvertToDouble(textDSNo1.Text);
            }
        }

        private void DSWtFormula2()  // D/S
        {
            if (comboBoxbottomtype2.SelectedIndex == 3) //Bottom Spout
            {
                if (checkBoxDoubleFoldBottomSpout2.Checked) //20.08.2021 add Double Fold
                {
                    if (checkFillerCord.Checked)
                        DSFabricSize2 = (Utility.SafeConvertToDouble(comboBoxbottomheight2.Text) + 18);
                    else
                        DSFabricSize2 = (Utility.SafeConvertToDouble(comboBoxbottomheight2.Text) + 12);
                    DSCutLenght2 = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) + 11);
                }
                else
                {
                    if (checkFillerCord.Checked)
                        DSFabricSize2 = (Utility.SafeConvertToDouble(comboBoxbottomheight2.Text) + 10);
                    else
                        DSFabricSize2 = (Utility.SafeConvertToDouble(comboBoxbottomheight2.Text) + 5);
                    DSCutLenght2 = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) + 4);
                }
                DSWt2 = DSCutLenght2 * DSFabricSize2 * (Utility.SafeConvertToDouble(comboBoxbottomgsm5.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM2.Text))
                    * Utility.SafeConvertToDouble(textDSNo2.Text); //+ 70000(Tie Wt) Remove by suggestion of Sabir on 4august 2008;
            }

            if (comboBoxbottomsubtype2.SelectedIndex == 2) //Iris Closure
            {
                double iris = 0;// (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) / 2) + 12.5 + Utility.SafeConvertToDouble(comboBoxbottomheight2.Text);
                if (checkBoxDoubleFoldBottomSpout2.Checked) //20.08.2021 add Double Fold
                {
                    iris = (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) / 2) + 18 + Utility.SafeConvertToDouble(comboBoxbottomheight2.Text);
                    DSFabricSize2 = iris;
                    DSCutLenght2 = (3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) + 11));
                }
                else
                {
                    iris = (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) / 2) + 12.5 + Utility.SafeConvertToDouble(comboBoxbottomheight2.Text);
                    DSFabricSize2 = iris;
                    DSCutLenght2 = (3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) + 4));
                }
                DSWt2 = (DSCutLenght2 * iris * (Utility.SafeConvertToDouble(comboBoxbottomgsm5.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM2.Text)))
                    * Utility.SafeConvertToDouble(textDSNo2.Text);   // + 160000;
            }

            if (comboBoxbottomsubtype2.SelectedIndex == 1 || comboBoxbottomsubtype2.SelectedIndex == 0) //Petal Closure,Simple
            {
                if (checkBoxDoubleFoldBottomSpout2.Checked) //20.08.2021 add Double Fold
                {
                    if (checkFillerCord.Checked && comboBoxbottomtype2.SelectedIndex == 3)
                        DSFabricSize2 = (Utility.SafeConvertToDouble(comboBoxbottomheight2.Text) + 18);
                    else
                        DSFabricSize2 = (Utility.SafeConvertToDouble(comboBoxbottomheight2.Text) + 12);

                    DSCutLenght2 = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) + 11);
                }
                else
                {
                    if (checkFillerCord.Checked && comboBoxbottomtype2.SelectedIndex == 3)
                        DSFabricSize2 = (Utility.SafeConvertToDouble(comboBoxbottomheight2.Text) + 10);
                    else
                        DSFabricSize2 = (Utility.SafeConvertToDouble(comboBoxbottomheight2.Text) + 5);

                    DSCutLenght2 = 3.14 * (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) + 4);
                }
                DSWt2 = DSCutLenght2 * DSFabricSize2 * (Utility.SafeConvertToDouble(comboBoxbottomgsm5.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM2.Text))
                    * Utility.SafeConvertToDouble(textDSNo2.Text);
            }
            if (_BodyIndex1 == 9) // Conical bag Three Piece
            {
                double OneSideDia = (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) * 3.14) / 4;
                if (checkBoxDoubleFoldBottomSpout2.Checked) //20.08.2021 add Double Fold
                {
                    // Body Spout Formula
                    DSCutLenght2 = (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) + 11) * 3.14;
                    DSFabricSize2 = Utility.SafeConvertToDouble(comboBoxbottomheight2.Text) + 12;
                }
                else
                {
                    // Body Spout Formula
                    DSCutLenght2 = (Utility.SafeConvertToDouble(comboBoxbottomdia2.Text) + 4) * 3.14;
                    DSFabricSize2 = Utility.SafeConvertToDouble(comboBoxbottomheight2.Text) + 5;
                }
                DSWt2 = DSCutLenght2 * DSFabricSize2 * (Utility.SafeConvertToDouble(comboBoxbottomgsm5.Text) + Utility.SafeConvertToDouble(comboBoxBottomSubTypeLamiGSM2.Text))
                * Utility.SafeConvertToDouble(textDSNo2.Text);
            }
        }

        /// <summary>
        /// BuffleWtFormula
        /// Change date : 17.Jun.2021
        /// change Date : 19.Jun.2021 for Almatis [Wider Fold]
        /// </summary>
        private void BuffleWtFormula()
        {
            double bufflewidth = 0;
            double BuffleHeight = 0;
            if (comboBody2.SelectedIndex == 12 || comboBody2.SelectedIndex == 13) //Almatis [Wider Fold] /Almatis [Std Fold] 19.06.2021
            {
                if (comboBuType.SelectedIndex == 0) //Standard
                {
                    if (_Type == 0) //Internal
                    {
                        bufflewidth = Math.Sqrt((_BagLenght / 3) * (_BagLenght / 3)
                                     + (_BagWidth / 3) * (_BagWidth / 3)) + 10;
                        BuffleHeight = _BagHeight - 20;
                    }
                    else //External
                    {
                        bufflewidth = Math.Sqrt(((_BagLenght - 5) / 3) * ((_BagLenght - 5) / 3)
                                                      + ((_BagWidth - 5) / 3) * ((_BagWidth - 5) / 3)) + 10;
                        BuffleHeight = _BagHeight - 25;
                    }
                    double BuffleGSM = 0;
                    double SingleCoatedGSM = 0;
                    double DoubleCoatedGSM = 0;

                    if (textBuffleGSM.Text != "")
                        BuffleGSM = Utility.SafeConvertToDouble(textBuffleGSM.Text);
                    if (textSingleCoatedGSM.Text != "")
                        SingleCoatedGSM = Utility.SafeConvertToDouble(textSingleCoatedGSM.Text);
                    if (textDoubleCoatedGSM.Text != "")
                        DoubleCoatedGSM = Utility.SafeConvertToDouble(textDoubleCoatedGSM.Text);
                    BuffleGSM = BuffleGSM + SingleCoatedGSM + DoubleCoatedGSM;
                    BuffleFabricSize = Math.Round(bufflewidth, 1);
                    if (textBufflecutlenght.Text == "")
                        BuffleCutLenght = Math.Round(BuffleHeight, 1);
                    else
                        BuffleCutLenght = Utility.SafeConvertToDouble(textBufflecutlenght.Text);

                    BuffleWt = bufflewidth * BuffleHeight * BuffleGSM * 4;
                }
                else if (comboBuType.SelectedIndex == 3) //Special
                {
                    if (_Type == 0) //Internal
                    {
                        bufflewidth = Math.Sqrt((Utility.SafeConvertToDouble(txtBuffSideA.Text) * Utility.SafeConvertToDouble(txtBuffSideA.Text)) +
                            (Utility.SafeConvertToDouble(txtBuffSideB.Text) * Utility.SafeConvertToDouble(txtBuffSideB.Text))) + 10;

                        BuffleHeight = _BagHeight - 20;
                        double BuffleGSM = 0;
                        double SingleCoatedGSM = 0;
                        double DoubleCoatedGSM = 0;

                        if (textBuffleGSM.Text != "")
                            BuffleGSM = Utility.SafeConvertToDouble(textBuffleGSM.Text);
                        if (textSingleCoatedGSM.Text != "")
                            SingleCoatedGSM = Utility.SafeConvertToDouble(textSingleCoatedGSM.Text);
                        if (textDoubleCoatedGSM.Text != "")
                            DoubleCoatedGSM = Utility.SafeConvertToDouble(textDoubleCoatedGSM.Text);
                        BuffleGSM = BuffleGSM + SingleCoatedGSM + DoubleCoatedGSM;
                        BuffleFabricSize = Math.Round(bufflewidth, 1);
                        if (textBufflecutlenght.Text == "")
                            BuffleCutLenght = Math.Round(BuffleHeight, 1);
                        else
                            BuffleCutLenght = Utility.SafeConvertToDouble(textBufflecutlenght.Text);

                        BuffleWt = bufflewidth * BuffleHeight * BuffleGSM * 4;
                    }
                }
                else if (comboBuType.SelectedIndex == 1) //Net Buffle 17.06.2021 new Condition added
                {
                    if (cmbSubBufType.SelectedIndex == 0)
                    {
                        if (_Type == 0) //Internal
                        {
                            #region change 17.06.2021
                            bufflewidth = Math.Sqrt((_BagLenght / 3) * (_BagLenght / 3)
                                         + (_BagWidth / 3) * (_BagWidth / 3)) + 2;
                            BuffleHeight = _BagHeight - 20;

                            #endregion
                        }
                        else //External
                        {
                            #region change 17.06.2021
                            bufflewidth = Math.Sqrt(((_BagLenght - 5) / 3) * ((_BagLenght - 5) / 3)
                                                          + ((_BagWidth - 5) / 3) * ((_BagWidth - 5) / 3)) + 2;
                            BuffleHeight = _BagHeight - 25;
                            #endregion
                        }
                        BuffleFabricSize = Math.Round(bufflewidth, 1);
                        if (textBufflecutlenght.Text == "")
                            BuffleCutLenght = Math.Round(BuffleHeight, 1);
                        else
                            BuffleCutLenght = Utility.SafeConvertToDouble(textBufflecutlenght.Text);
                        BuffleWt = (BuffleHeight * 50) / 100 * 4; //17.06.2021
                    }
                    else
                    {
                        //if (_Type == 0) //Internal comment as both are same Internal & external 10.08.2021
                        {
                            bufflewidth = Math.Sqrt((Utility.SafeConvertToDouble(txtBuffSideA.Text) * Utility.SafeConvertToDouble(txtBuffSideA.Text)) +
                                (Utility.SafeConvertToDouble(txtBuffSideB.Text) * Utility.SafeConvertToDouble(txtBuffSideB.Text))) + 2;

                            BuffleHeight = _BagHeight - 20;

                            BuffleFabricSize = Math.Round(bufflewidth, 1);
                            if (textBufflecutlenght.Text == "")
                                BuffleCutLenght = Math.Round(BuffleHeight, 1);
                            else
                                BuffleCutLenght = Utility.SafeConvertToDouble(textBufflecutlenght.Text);

                            BuffleWt = (BuffleHeight * 50) / 100 * 4;
                        }
                    }
                }
            }
            else
            {
                if (comboBuType.SelectedIndex == 0) //Standard
                {
                    if (_Type == 0) //Internal
                    {
                        bufflewidth = Math.Sqrt((_BagLenght / 3) * (_BagLenght / 3)
                                     + (_BagWidth / 3) * (_BagWidth / 3)) + 10;
                        //  bufflewidth = Math.Sqrt((_BagLenght / 3)                             +   (_BagWidth / 3)) + 10;
                        BuffleHeight = _BagHeight - 20;
                    }
                    else //External
                    {
                        bufflewidth = Math.Sqrt(((_BagLenght - 5) / 3) * ((_BagLenght - 5) / 3)
                                                      + ((_BagWidth - 5) / 3) * ((_BagWidth - 5) / 3)) + 10;
                        BuffleHeight = _BagHeight - 25;
                    }
                    double BuffleGSM = 0;
                    double SingleCoatedGSM = 0;
                    double DoubleCoatedGSM = 0;

                    if (textBuffleGSM.Text != "")
                        BuffleGSM = Utility.SafeConvertToDouble(textBuffleGSM.Text);
                    if (textSingleCoatedGSM.Text != "")
                        SingleCoatedGSM = Utility.SafeConvertToDouble(textSingleCoatedGSM.Text);
                    if (textDoubleCoatedGSM.Text != "")
                        DoubleCoatedGSM = Utility.SafeConvertToDouble(textDoubleCoatedGSM.Text);
                    BuffleGSM = BuffleGSM + SingleCoatedGSM + DoubleCoatedGSM;
                    BuffleFabricSize = Math.Round(bufflewidth, 1);
                    if (textBufflecutlenght.Text == "")
                        BuffleCutLenght = Math.Round(BuffleHeight, 1);
                    else
                        BuffleCutLenght = Utility.SafeConvertToDouble(textBufflecutlenght.Text);

                    BuffleWt = bufflewidth * BuffleHeight * BuffleGSM * 4; //19.06.2021


                }
                else if (comboBuType.SelectedIndex == 1) //Net Buffle 17.06.2021 new Condition added
                {
                    if (cmbSubBufType.SelectedIndex == 0)
                    {
                        if (_Type == 0) //Internal
                        {
                            #region change 17.06.2021
                            bufflewidth = Math.Sqrt((_BagLenght / 3) * (_BagLenght / 3)
                                         + (_BagWidth / 3) * (_BagWidth / 3)) + 2;
                            BuffleHeight = _BagHeight - 20;

                            #endregion
                        }
                        else //External
                        {
                            #region change 17.06.2021
                            bufflewidth = Math.Sqrt(((_BagLenght - 5) / 3) * ((_BagLenght - 5) / 3)
                                                          + ((_BagWidth - 5) / 3) * ((_BagWidth - 5) / 3)) + 2;
                            BuffleHeight = _BagHeight - 25;
                            #endregion
                        }
                        BuffleFabricSize = Math.Round(bufflewidth, 1);
                        if (textBufflecutlenght.Text == "")
                            BuffleCutLenght = Math.Round(BuffleHeight, 1);
                        else
                            BuffleCutLenght = Utility.SafeConvertToDouble(textBufflecutlenght.Text);
                        BuffleWt = (BuffleHeight * 50) / 100 * 4; //17.06.2021
                    }
                    else
                    {
                        if (_Type == 0) //Internal
                        {
                            bufflewidth = Math.Sqrt((Utility.SafeConvertToDouble(txtBuffSideA.Text) * Utility.SafeConvertToDouble(txtBuffSideA.Text)) +
                                (Utility.SafeConvertToDouble(txtBuffSideB.Text) * Utility.SafeConvertToDouble(txtBuffSideB.Text))) + 2;

                            BuffleHeight = _BagHeight - 20;

                            BuffleFabricSize = Math.Round(bufflewidth, 1);
                            if (textBufflecutlenght.Text == "")
                                BuffleCutLenght = Math.Round(BuffleHeight, 1);
                            else
                                BuffleCutLenght = Utility.SafeConvertToDouble(textBufflecutlenght.Text);

                            BuffleWt = (BuffleHeight * 50) / 100 * 4;
                        }
                    }
                }
                else if (comboBuType.SelectedIndex == 2) //Rectangle 17.06.2021 new Condition added
                {
                    if (_Type == 0) //Internal
                    {
                        bufflewidth = Math.Sqrt(
                            (Utility.SafeConvertToDouble(txtBuffSideA.Text) * Utility.SafeConvertToDouble(txtBuffSideA.Text)) +
                            (Utility.SafeConvertToDouble(txtBuffSideB.Text) * Utility.SafeConvertToDouble(txtBuffSideB.Text))) + 10;

                        BuffleHeight = _BagHeight - 20;
                        BuffleFabricSize = Math.Round(bufflewidth, 1);
                        if (textBufflecutlenght.Text == "")
                            BuffleCutLenght = Math.Round(BuffleHeight, 1);
                        else
                            BuffleCutLenght = Utility.SafeConvertToDouble(textBufflecutlenght.Text);

                        double BuffleGSM = 0;
                        double SingleCoatedGSM = 0;
                        double DoubleCoatedGSM = 0;

                        if (textBuffleGSM.Text != "")
                            BuffleGSM = Utility.SafeConvertToDouble(textBuffleGSM.Text);
                        if (textSingleCoatedGSM.Text != "")
                            SingleCoatedGSM = Utility.SafeConvertToDouble(textSingleCoatedGSM.Text);
                        if (textDoubleCoatedGSM.Text != "")
                            DoubleCoatedGSM = Utility.SafeConvertToDouble(textDoubleCoatedGSM.Text);
                        BuffleGSM = BuffleGSM + SingleCoatedGSM + DoubleCoatedGSM;
                        BuffleFabricSize = Math.Round(bufflewidth, 1);
                        if (textBufflecutlenght.Text == "")
                            BuffleCutLenght = Math.Round(BuffleHeight, 1);
                        else
                            BuffleCutLenght = Utility.SafeConvertToDouble(textBufflecutlenght.Text);

                        BuffleWt = bufflewidth * BuffleHeight * BuffleGSM * 4;
                    }
                }
            }
            //140 
            //}
            //    double BuffleGSM = 0;
            //    double SingleCoatedGSM = 0;
            //    double DoubleCoatedGSM = 0;
            //    if (textBuffleGSM.Text != "")
            //        BuffleGSM = Utility.SafeConvertToDouble(textBuffleGSM.Text);
            //    if (textSingleCoatedGSM.Text != "")
            //        SingleCoatedGSM = Utility.SafeConvertToDouble(textSingleCoatedGSM.Text);
            //    if (textDoubleCoatedGSM.Text != "")
            //        DoubleCoatedGSM = Utility.SafeConvertToDouble(textDoubleCoatedGSM.Text);

            //    BuffleGSM = BuffleGSM + SingleCoatedGSM + DoubleCoatedGSM;

            //    BuffleFabricSize = bufflewidth;
            //    BuffleCutLenght = BuffleHeight;

            //    BuffleFabricSize = Math.Round(BuffleFabricSize, 1);
            //    BuffleCutLenght = Math.Round(BuffleCutLenght, 1);

            //    BuffleWt = bufflewidth * BuffleHeight * BuffleGSM * 4;
            //}
        }
        private void ThreadWtFormula()
        {
            ThreadWt = 0;

            if (_Type == 0) //Internal
            {
                if (checkBoxTop.Checked)
                    ThreadWt += (_BagLenght
                                  + _BagWidth + 10) * 2;
                if (checkbottom.Checked && _BodyIndex1 != 0)
                    ThreadWt += (_BagLenght
                              + _BagWidth + 10) * 2;
            }

            else if (_Type == 1) // External
            {
                if (checkBoxTop.Checked)
                    ThreadWt += (_BagLenght
                                  + _BagWidth) * 2;
                if (checkbottom.Checked && _BodyIndex1 != 0)
                    ThreadWt += (_BagLenght
                              + _BagWidth) * 2;
            }

            if (comboTopType.SelectedIndex == 1) // Top SPout
                ThreadWt += ((3.14 * Utility.SafeConvertToDouble(comboSpoutDia.Text))
                      + 12 + Utility.SafeConvertToDouble(comboSpoutHeight.Text) + 10)*2;

            if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 6 ||
                comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8 || comboTopType.SelectedIndex == 9) // Duffle or Skirt 17.06.2021
            {
                if (_Type == 0)
                    ThreadWt += (Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + 5) * 2;
                else
                    ThreadWt += Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) * 2;
            }

            if (comboBoxbottomtype.SelectedIndex == 3)// Bottom SPout
            {
                ThreadWt += ((3.14 * Utility.SafeConvertToDouble(comboBoxbottomdia.Text))
                                      + 12 + 10 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text))*2;
               
            }

            if (_BodyIndex1 == 0) //Upanel
            {
                if (checkBoxTunnel.Checked)
                {
                    if (_Type == 0) // Internal
                        ThreadWt += (_BagHeight * 2 + _BagLenght + 10) * 2 + 150;
                    else if (_Type == 1) // External
                        ThreadWt += (_BagHeight * 2 + _BagLenght) * 2 + 140;

                    ThreadWt += _BagWidth * 4;
                }
                else
                {
                    if (_Type == 0) // Internal
                        ThreadWt += (_BagHeight * 2 + _BagLenght + 10) * 2;
                    else if (_Type == 1) // External
                        ThreadWt += (_BagHeight * 2 + _BagLenght) * 2;
                }

                if (_Type == 0)
                    ThreadWt += (_BagHeight + 5) * 4; 
                 else if (_Type == 1) // External
                    ThreadWt += (_BagHeight) * 4; 
            }

            else if (_BodyIndex1 == 1 || _BodyIndex1 == 5 || _BodyIndex1 == 6) // Circular
            {

            }

            else if (_BodyIndex1 == 2) // Buffle
            {
                if (_Type == 0) // Internal
                {
                    if (comboThreadBuffleSeam.SelectedIndex == 0) // All Seam
                        ThreadWt += (_BagHeight + 5) * 16;
                    if (comboThreadBuffleSeam.SelectedIndex == 1) //Eight Seam
                        ThreadWt += (_BagHeight + 5) * 12;
                    if (comboThreadBuffleSeam.SelectedIndex == 2) //Bag Corner Seam
                        ThreadWt += (_BagHeight + 5) * 8;
                }
                else if (_Type == 1) //External
                {
                    if (comboThreadBuffleSeam.SelectedIndex == 0) // All Seam
                        ThreadWt += _BagHeight * 16;
                    if (comboThreadBuffleSeam.SelectedIndex == 1) //Eight Seam
                        ThreadWt += _BagHeight * 12;
                    if (comboThreadBuffleSeam.SelectedIndex == 2) //Bag Corner Seam
                        ThreadWt += _BagHeight *8;
                }
            }

            else if (_BodyIndex1 == 3 || _BodyIndex1 == 4 ||
                     _BodyIndex1 == 7 || _BodyIndex1 == 8 ||
                     _BodyIndex1 == 9 || _BodyIndex1 == 10) // 4 Panel,Tube + Corner
            {
                if (_Type == 0) // Internal
                    ThreadWt += ((_BagHeight + 5) * 4) * 2;

                else if (_Type == 1) //External
                    ThreadWt += (_BagHeight * 4) * 2;


            }

            if (comboThreadNeedle.SelectedIndex == 1) // Double Needle
                ThreadWt = ThreadWt * 2;
            if (checkBoxTopBand.Checked )
            {
                if (_Type == 0) //Internal
                    ThreadWt += ((_BagLenght + _BagWidth) * 2) + 20;
                else
                    ThreadWt += ((_BagLenght + _BagWidth) * 2) + 15;
            }
            

            if (checkStevdore.Checked)
            {
                if (comboStPortion.SelectedIndex == 0) // Lenght Portion
                {
                    if (_Type == 0) // Internal
                       ThreadWt +=  (_BagLenght * 2 + 20) *2 *Utility.SafeConvertToDouble(textStNo.Text);
                       
                    else
                        ThreadWt += (_BagLenght * 2 + 15) * 2 * Utility.SafeConvertToDouble(textStNo.Text);
                }
                else if (comboStPortion.SelectedIndex == 1) // Width Portion
                {
                    if (_Type == 0) // Internal
                        ThreadWt += (_BagWidth * 2 + 20) * 2 * Utility.SafeConvertToDouble(textStNo.Text);
                    else
                        ThreadWt += (_BagWidth * 2 + 15) * 2 * Utility.SafeConvertToDouble(textStNo.Text);
                }
                else if (comboStPortion.SelectedIndex == 2) // Diagonal Portion
                {
                    if (_Type == 0) // Internal
                        ThreadWt += (Math.Sqrt((_BagWidth * _BagWidth) + (_BagLenght * _BagLenght)) * 2 + 20) * 2 * Utility.SafeConvertToDouble(textStNo.Text);
                    else
                        ThreadWt += (Math.Sqrt((_BagWidth * _BagWidth) + (_BagLenght * _BagLenght)) * 2 + 15) * 2 * Utility.SafeConvertToDouble(textStNo.Text);

                }
            }

            ThreadWt = (ThreadWt * 2.85);

            if (comboLoopConst.SelectedIndex == 2) // Cross Corner
            {
                if (Utility.SafeConvertToDouble(textSWL.Text) >= 500 && Utility.SafeConvertToDouble(textSWL.Text) <= 1250)
                    ThreadWt += 2000;
                else if (Utility.SafeConvertToDouble(textSWL.Text) > 1250)
                    ThreadWt += 2500;
            }

            if (checkHiracle.Checked)
            {
                if (_Type == 0) //Internal
                {
                    if (checkHiracleTop.Checked)
                        ThreadWt += (_BagLenght
                                      + _BagWidth + 10) * 2 * 4;
                    if (checkHiracleBottom.Checked)
                    {
                        if(_BodyIndex1 == 2) //buffle
                            ThreadWt += (_BagLenght + _BagWidth + 10) * 2 * 4;
                        else
                            ThreadWt += (_BagLenght * 2 + 10)  * 4;
                    }
                }

                else if (_Type == 1) // External
                {
                    if (checkHiracleTop.Checked)
                        ThreadWt += (_BagLenght + _BagWidth) * 2 * 4;
                    if (checkHiracleBottom.Checked)
                    {
                        if (_BodyIndex1 == 2) //buffle
                            ThreadWt += (_BagLenght + _BagWidth) * 2 * 4;
                        else
                            ThreadWt += (_BagLenght * 2) * 4;
                    }
                }

                if (_BodyIndex1 == 0) //Upanel
                {
                    if (checkBoxTunnel.Checked)
                    {
                        if (_Type == 0) // Internal
                            ThreadWt += ((_BagHeight + 5) * 2 + _BagLenght + 5) * 2 * 4;
                        else if (_Type == 1) // External
                            ThreadWt += (_BagHeight * 2 + _BagLenght) * 2 * 4;
                    }
                    else
                    {
                        if (_Type == 0) // Internal
                            ThreadWt += (_BagHeight + 10) * 4*4;
                        else if (_Type == 1) // External
                            ThreadWt += _BagHeight * 4*4;
                    }
                }

                else if (_BodyIndex1 == 1 || _BodyIndex1 == 5 || _BodyIndex1 == 6) // Circular
                {

                }

                else if (_BodyIndex1 == 2) // Buffle
                {
                    if (_Type == 0) // Internal
                    {
                        //if (comboThreadBuffleSeam.SelectedIndex == 0) // All Seam
                        //    ThreadWt += (_BagHeight + 5) * 12 * 4;
                        //if (comboThreadBuffleSeam.SelectedIndex == 1) //Eight Seam
                        //    ThreadWt += (_BagHeight + 5) * 8 * 4;
                        //if (comboThreadBuffleSeam.SelectedIndex == 2) //Bag Corner Seam
                            ThreadWt += (_BagHeight + 5) * 4 * 4;
                    }
                    else if (_Type == 1) //External
                    {
                        //if (comboThreadBuffleSeam.SelectedIndex == 0) // All Seam
                        //    ThreadWt += _BagHeight * 12 * 4;
                        //if (comboThreadBuffleSeam.SelectedIndex == 1) //Eight Seam
                        //    ThreadWt += _BagHeight * 8 * 4;
                        //if (comboThreadBuffleSeam.SelectedIndex == 2) //Bag Corner Seam
                            ThreadWt += _BagHeight * 4 * 4;
                    }

                }

                else if (_BodyIndex1 == 3 || _BodyIndex1 == 4 ||
                         _BodyIndex1 == 7 || _BodyIndex1 == 8 ||
                         _BodyIndex1 == 9 || _BodyIndex1 == 10) // 4 Panel,Tube + Corner
                {
                    if (_Type == 0) // Internal
                        ThreadWt += (_BagHeight + 5) * 4 * 4;

                    else if (_Type == 1) //External
                        ThreadWt += (_BagHeight * 4) * 4;
                }
            }
        }
        private void SafetyBandWtFormula()
        {
            SafetyBandWt = (_BagLenght + _BagHeight) * 2 * 25;
        }
        private void comboBody1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _BodyIndex1 = comboBody1.SelectedIndex;
            checkBoxRF.Checked = false;
            if (_BodyIndex1 == 5 || _BodyIndex1 == 6
                || _BodyIndex1 == 7 || _BodyIndex1 == 8)
                groupSingleLoop.Visible = true;
            else
                groupSingleLoop.Visible = false;

            if (_BodyIndex1 == 2) // Buffle
            {
                comboBuffleType.Visible = true;
                comboBuffleType.SelectedIndex = 0;
                groupBuffleSeam.Visible = true;
                comboBuType.Text = "Standard";

            }
            else
            {
                comboBuffleType.Visible = false;
                groupBuffleSeam.Visible = false;
            }

            if (_BodyIndex1 == 0)
                checkSide.Checked = true;
            else
                checkSide.Checked = false;
            if (_BodyIndex1 == 1)
                checkBoxRF.Checked = false; // For Circular bag default check mark is removed as per email from Marketing. 06.01.2022
            else
                checkBoxRF.Checked = false;

            if (_BodyIndex1 == 5 || _BodyIndex1 == 6
                  || _BodyIndex1 == 7 || _BodyIndex1 == 8)
                groupSingleLoop.Visible = true;
            else
                groupSingleLoop.Visible = false;
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (FilePONo != "" && FilePONo == textFilePONo.Text)
            {
                MessageBox.Show("Please change Qtn No/File PONO to Save As this BOM Or Use Update button to change in Existing BOM");
                return;
            }
            if (comboBagType.Text.Length == 0)
            {
                MessageBox.Show("Please Select Bag Type");
                comboBagType.Focus();
                return;
            }

            int count = 0;
            for (int i = 0; i < dgapprovallist.Rows.Count - 1; i++)
            {
                if (dgapprovallist.Rows[i].Cells[0].FormattedValue.ToString() == "True")
                {
                    count++;
                }
            }

            if (count == 0)
            {
                MessageBox.Show("Please Select Atleast one item from Approval List");
                dataGridView1.Focus();
                return;
            }

            TotalKg = 0;
            if (Database.OpenConnection(Utility.ProductionConnectionString))
            {

                bool isPoAllow = true;
                Database.myreader = Database.GetExecuteReaderCommand("select Srno from BOM1   where  FilePONo = '" + textFilePONo.Text + "'  and Srno != 'temp' ");
                if (Database.myreader.Read())
                {
                    isPoAllow = false;
                    MessageBox.Show("This Qtn No is already stored in Database ");
                }
                Database.myreader.Close();

                //}
                if (isPoAllow == true)
                {
                    {
                        Database.myreader.Close();
                        DialogResult dialog = MessageBox.Show("Do you want to save PONo " + textFilePONo.Text, "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        if (dialog.CompareTo(DialogResult.Yes) == 0)
                        {
                            if (FilePONo == "")
                            {
                                FilePONo = textFilePONo.Text;
                                CompanyName = comboPartyName.Text;
                            }
                            else
                            {
                                dialog = MessageBox.Show("Do you want to save this PONo against same Customer " + textFilePONo.Text, "Save",
                                          MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (dialog.CompareTo(DialogResult.Yes) == 0 && CompanyName == comboPartyName.Text)
                                    FilePONo += ";" + textFilePONo.Text;
                                else
                                {
                                    FilePONo = textFilePONo.Text;
                                    CompanyName = comboPartyName.Text;
                                }
                            }
                            string SrNo = "";

                            SrNo = Convert.ToString(System.Guid.NewGuid());

                            IsTemp = false;

                            print(SrNo);

                            Utility.UserInformation("Save ", FrmMainForm.UserName, "BOM No with Quotation No " + textFilePONo.Text + " saved successfully");

                            FilePONo = "";
                        }
                    }
                }
                Database.Closeconnection();
            }

        }


        private void InnerTopWtFormula()
        {

            if (_BodyIndex1 == 5 || _BodyIndex1 == 6 || _BodyIndex1 == 1) // Single Loop & Double Loop & circular
            {
                InnerTopFabricSize = (_BagLenght + 12);
                InnerTopCutLenght = (_BagWidth + 12) + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                InnerTopWt = InnerTopCutLenght * InnerTopFabricSize * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text));
            }
            else if (_BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4
                || _BodyIndex1 == 7 || _BodyIndex1 == 8)
            {
                if (_Type == 0) //Internal
                {
                    InnerTopFabricSize = (_BagLenght + 12);
                    InnerTopCutLenght = (_BagWidth + 12) + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                    InnerTopWt = InnerTopCutLenght * InnerTopFabricSize * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text));
                }
                else if (_Type == 1) //External
                {
                    InnerTopFabricSize = _BagLenght + 8;
                    InnerTopCutLenght = _BagWidth + 8 + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                    InnerTopWt = InnerTopCutLenght * InnerTopFabricSize * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text));
                }
            }
            else if (_BodyIndex1 == 9) //Conical Bag Three Piece
            {
                double OneSideDia = (Utility.SafeConvertToDouble(comboInnerTopDia.Text) * 3.14) / 4;
                if (_Type == 0) //Internal
                {
                    InnerTopFabricSize = _BagLenght + 12;
                    InnerTopCutLenght = ((_BagWidth - OneSideDia) / 2) + 14 + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                    InnerTopWt = InnerTopCutLenght * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text)) * 4 * InnerTopFabricSize;
                }
                else if (_Type == 1) //External
                {
                    InnerTopFabricSize = _BagLenght + 8;
                    InnerTopCutLenght = ((_BagWidth - OneSideDia) / 2) + 10 + +Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                    InnerTopWt = InnerTopCutLenght * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text)) * 4 * InnerTopFabricSize;
                }
            }

            if (_BodyIndex1 == 0 || _BodyIndex1 == 1 || _BodyIndex1 == 2
                 || _BodyIndex1 == 3 || _BodyIndex1 == 4 || _BodyIndex1 == 9
                 || _BodyIndex1 == 13)
            {
                if (comboTopType.SelectedIndex == 3) //Conical Plate
                {
                    if (_Type == 0) //Internal
                    {
                        InnerTopFabricSize = _BagLenght + 32;
                        InnerTopCutLenght = _BagWidth + 32 + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                        InnerTopWt = InnerTopCutLenght * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text)) * InnerTopFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        InnerTopFabricSize = _BagLenght + 28;
                        InnerTopCutLenght = _BagWidth + 28;
                        InnerTopWt = InnerTopCutLenght * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text)) * InnerTopFabricSize;
                    }
                }
                else if (comboTopType.SelectedIndex == 4) //Conical Top
                {
                    double OneSideDia = (Utility.SafeConvertToDouble(comboInnerTopDia.Text) * 3.14) / 4;
                    if (_Type == 0) //Internal
                    {
                        InnerTopFabricSize = _BagLenght + 12;
                        InnerTopCutLenght = ((_BagWidth - OneSideDia) / 2) + 14 + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                        InnerTopWt = InnerTopCutLenght * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text)) * 4 * InnerTopFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        InnerTopFabricSize = _BagLenght + 8;
                        InnerTopCutLenght = ((_BagWidth - OneSideDia) / 2) + 10 + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                        InnerTopWt = InnerTopCutLenght * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text)) * 4 * InnerTopFabricSize;
                    }
                }
                else if (comboTopType.SelectedIndex == 1) //TopSpout
                {
                    if (_Type == 0) //Internal
                    {
                        InnerTopFabricSize = (_BagLenght + 12);
                        InnerTopCutLenght = (_BagWidth + 12) + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                        InnerTopWt = InnerTopCutLenght * InnerTopFabricSize * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text));
                    }
                    else if (_Type == 1) //External
                    {
                        InnerTopFabricSize = _BagLenght + 8;
                        InnerTopCutLenght = _BagWidth + 8 + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                        InnerTopWt = InnerTopCutLenght * InnerTopFabricSize * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text));
                    }
                }
                else if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 6 ||
                    comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8 || comboTopType.SelectedIndex == 9) //Duffle 17.06.2021
                {
                    if (_Type == 0) //Internal
                    {
                        InnerTopFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + 5;
                        InnerTopCutLenght = (_BagLenght * 4) + 12 + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                        InnerTopWt = InnerTopCutLenght * InnerTopFabricSize * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text));
                    }
                    if (_Type == 1) //External
                    {
                        InnerTopFabricSize = Utility.SafeConvertToDouble(comboBoxduffleskirtheight.Text) + 5;
                        InnerTopCutLenght = ((_BagLenght - 4) * 4) + 12 + Utility.SafeConvertToDouble(textInnerTopExtra.Text);
                        InnerTopWt = InnerTopCutLenght * InnerTopFabricSize * (Utility.SafeConvertToDouble(comboInnerTopGSM.Text) + Utility.SafeConvertToDouble(comboInnerTopLamiGSM.Text));
                    }
                }
            }
        }
        private void InnerBottomWtFormula()
        {
            if (_BodyIndex1 == 5 || _BodyIndex1 == 6 || _BodyIndex1 == 1) // Single Loop & Double Loop & circular
            {
                InnerBottomFabricSize = (_BagLenght + 12);
                InnerBottomCutLenght = (_BagWidth + 12) + Utility.SafeConvertToDouble(textInnerBottomExtra.Text);
                double TotalGSM = (Utility.SafeConvertToDouble(comboInnerBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboInnerBottomGSM.Text));
                InnerBottomWt = InnerBottomCutLenght * InnerBottomFabricSize * TotalGSM;
            }
            else if (_BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4
                || _BodyIndex1 == 7 || _BodyIndex1 == 8 || _BodyIndex1 == 13)
            {

                InnerBottomFabricSize = (_BagLenght + 12);
                InnerBottomCutLenght = (_BagWidth + 12) + Utility.SafeConvertToDouble(textInnerBottomExtra.Text);
                InnerBottomWt = InnerBottomCutLenght * InnerBottomFabricSize * (Utility.SafeConvertToDouble(comboInnerBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboInnerBottomGSM.Text));

            }
            else if (_BodyIndex1 == 9) //Conical Bag Three Piece
            {
                double OneSideDia = (Utility.SafeConvertToDouble(comboInnerBottomDia.Text) * 3.14) / 4;
                if (_Type == 0) //Internal
                {
                    InnerBottomFabricSize = _BagLenght + 12;
                    InnerBottomCutLenght = ((_BagWidth - OneSideDia) / 2) + 14 + Utility.SafeConvertToDouble(textInnerBottomExtra.Text);
                    InnerBottomWt = InnerBottomCutLenght * (Utility.SafeConvertToDouble(comboInnerBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboInnerBottomGSM.Text)) * 4 * InnerBottomFabricSize;
                }
                else if (_Type == 1) //External
                {
                    InnerBottomFabricSize = _BagLenght + 8;
                    InnerBottomCutLenght = ((_BagWidth - OneSideDia) / 2) + 10 + Utility.SafeConvertToDouble(textInnerBottomExtra.Text);
                    InnerBottomWt = InnerBottomCutLenght * (Utility.SafeConvertToDouble(comboInnerBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboInnerBottomGSM.Text)) * 4 * InnerBottomFabricSize;
                }
            }

            if (_BodyIndex1 == 1 || _BodyIndex1 == 2
                    || _BodyIndex1 == 3 || _BodyIndex1 == 4 || _BodyIndex1 == 9)
            {
                if (comboBoxbottomtype.SelectedIndex == 1) //Conical Plate
                {
                    if (_Type == 0) //Internal
                    {
                        InnerBottomFabricSize = _BagLenght + 32;
                        InnerBottomCutLenght = _BagWidth + 32 + Utility.SafeConvertToDouble(textInnerBottomExtra.Text);
                        InnerBottomWt = InnerBottomCutLenght * (Utility.SafeConvertToDouble(comboInnerBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboInnerBottomGSM.Text)) * InnerBottomFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        InnerBottomFabricSize = _BagLenght + 28;
                        InnerBottomCutLenght = _BagWidth + 28 + Utility.SafeConvertToDouble(textInnerBottomExtra.Text);
                        InnerBottomWt = InnerBottomCutLenght * (Utility.SafeConvertToDouble(comboInnerBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboInnerBottomGSM.Text)) * InnerBottomFabricSize;
                    }
                }
                else if (comboBoxbottomtype.SelectedIndex == 2) //Conical Bottom
                {
                    double OneSideDia = (Utility.SafeConvertToDouble(comboInnerBottomDia.Text) * 3.14) / 4;
                    if (_Type == 0) //Internal
                    {
                        InnerBottomFabricSize = _BagLenght + 12;
                        InnerBottomCutLenght = ((_BagWidth - OneSideDia) / 2) + 14 + Utility.SafeConvertToDouble(textInnerBottomExtra.Text);
                        InnerBottomWt = InnerBottomCutLenght * (Utility.SafeConvertToDouble(comboInnerBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboInnerBottomGSM.Text)) * 4 * InnerBottomFabricSize;
                    }
                    else if (_Type == 1) //External
                    {
                        InnerBottomFabricSize = _BagLenght + 8;
                        InnerBottomCutLenght = ((_BagWidth - OneSideDia) / 2) + 10 + Utility.SafeConvertToDouble(textInnerBottomExtra.Text);
                        InnerBottomWt = InnerBottomCutLenght * (Utility.SafeConvertToDouble(comboInnerBottomLamiGSM.Text) + Utility.SafeConvertToDouble(comboInnerBottomGSM.Text)) * 4 * InnerBottomFabricSize;
                    }
                }
            }
            if (_BodyIndex1 == 12) //Double Layer Tunnel Lift Loop Bag
            {
                if (_Type == 0) //Internal
                {
                    InnerBottomFabricSize = _BagLenght + 12;
                    InnerBottomCutLenght = _BagWidth + 12 + Utility.SafeConvertToDouble(textInnerBottomExtra.Text);
                    InnerBottomWt = InnerBottomCutLenght * (Utility.SafeConvertToDouble(comboInnerBottomLamiGSM.Text)
                        + Utility.SafeConvertToDouble(comboInnerBottomGSM.Text)) * InnerBottomFabricSize;
                }
                else if (_Type == 1) //External
                {
                    InnerBottomFabricSize = _BagLenght + 8;
                    InnerBottomCutLenght = _BagWidth + 8 + Utility.SafeConvertToDouble(textInnerBottomExtra.Text);
                    InnerBottomWt = InnerBottomCutLenght * (Utility.SafeConvertToDouble(comboInnerBottomLamiGSM.Text)
                        + Utility.SafeConvertToDouble(comboInnerBottomGSM.Text)) * InnerBottomFabricSize;
                }
            }
        }

        private double FillerCordWtFormula() //Dust Proof
        {
            FillerCordGSM = Utility.SafeConvertToDouble(TextFillerGPM.Text);// 4.5; change 17.06.2021
            FillerCordWt = 0;
            int addCM = 0;

            if (_Type == 0) //Internal
                addCM = 10;

            if (checkFillerTop.Checked) // for TOP
            {
                if (comboFillerCordTop.SelectedIndex == 1) // single
                    FillerCordWt += ((_BagLenght + _BagWidth + addCM) * 2);
                if (comboFillerCordTop.SelectedIndex == 2) // double
                    FillerCordWt += ((_BagLenght + _BagWidth + addCM) * 4);

            }
            if (checkFillerBottom.Checked) // Bottom
            {
                if (_BodyIndex1 == 0) //Upanel
                {
                    if (comboFillerCordBottom.SelectedIndex == 1) // single
                        FillerCordWt += (_BagWidth + addCM) * 2;
                    if (comboFillerCordBottom.SelectedIndex == 2) // double
                        FillerCordWt += (_BagWidth + addCM) * 4;
                }
                else
                {
                    if (comboFillerCordBottom.SelectedIndex == 1) // single
                        FillerCordWt += ((_BagLenght + _BagWidth + addCM) * 2);
                    if (comboFillerCordBottom.SelectedIndex == 2) // double
                        FillerCordWt += ((_BagLenght + _BagWidth + addCM) * 4);
                }
            }
            if (checkFillerTopSpout.Checked)
            {
                if (comboFillerCordTopS.SelectedIndex == 1) // single
                    FillerCordWt += (3.14 * Utility.SafeConvertToDouble(comboSpoutDia.Text)) + 12 + Utility.SafeConvertToDouble(comboSpoutHeight.Text);
                if (comboFillerCordTopS.SelectedIndex == 2) // double
                    FillerCordWt += ((3.14 * Utility.SafeConvertToDouble(comboSpoutDia.Text)) + 12 + Utility.SafeConvertToDouble(comboSpoutHeight.Text)) * 2;

            }

            if (checkFillerBottomSpout.Checked)
            {
                if (comboFillerCordBottomS.SelectedIndex == 1) // single
                    FillerCordWt += (3.14 * Utility.SafeConvertToDouble(comboBoxbottomdia.Text)) + 12 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text);
                if (comboFillerCordBottomS.SelectedIndex == 2) // double
                    FillerCordWt += ((3.14 * Utility.SafeConvertToDouble(comboBoxbottomdia.Text)) + 12 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text)) * 2;

            }
            if (CheckFillerBody.Checked && (_BodyIndex1 != 1 || _BodyIndex1 != 5 || _BodyIndex1 != 6))// Body. By Rikin on 12-02-2015 not for circular
            {
                if (_Type == 0) // Internal
                {
                    if (comboFillerCordBody.SelectedIndex == 1)
                        FillerCordWt += ((_BagHeight + addCM) * 4);
                    if (comboFillerCordBody.SelectedIndex == 2)
                        FillerCordWt += ((_BagHeight + addCM) * 8);
                }

                else if (_Type == 1) //External
                {
                    if (comboFillerCordBody.SelectedIndex == 1)
                        FillerCordWt += (_BagHeight * 4);
                    if (comboFillerCordBody.SelectedIndex == 2)
                        FillerCordWt += (_BagHeight * 8);
                }

            }


            if (_BodyIndex1 == 1 || _BodyIndex1 == 5 || _BodyIndex1 == 6) // Circular else 
            {

            }

            else if (_BodyIndex1 == 2) // Buffle
            {
                if (_Type == 0) // Internal
                {
                    if (comboFillerCordBuffle.SelectedIndex == 1)
                        FillerCordWt += (_BagHeight + 5) * 8;
                    if (comboFillerCordBuffle.SelectedIndex == 2)
                        FillerCordWt += (_BagHeight + 5) * 16;
                    //if (comboBuffleSeam.SelectedIndex == 0) // All Seam
                    //    FillerCordWt += (_BagHeight + 5) * 12;
                    //else if (comboBuffleSeam.SelectedIndex == 1) //Eight Seam
                    //    FillerCordWt += (_BagHeight + 5) * 8;
                    //else if (comboBuffleSeam.SelectedIndex == 2) //Bag Corner Seam
                    //    FillerCordWt += (_BagHeight + 5) * 4;
                }
                else if (_Type == 1) //External
                {
                    if (comboFillerCordBuffle.SelectedIndex == 1)
                        FillerCordWt += (_BagHeight) * 8;
                    if (comboFillerCordBuffle.SelectedIndex == 2)
                        FillerCordWt += (_BagHeight) * 16;
                    //if (comboBuffleSeam.SelectedIndex == 0) // All Seam
                    //    FillerCordWt += _BagHeight * 12;
                    //else if (comboBuffleSeam.SelectedIndex == 1) //Eight Seam
                    //    FillerCordWt += _BagHeight * 8;
                    //else if (comboBuffleSeam.SelectedIndex == 2) //Bag Corner Seam
                    //    FillerCordWt += _BagHeight * 4;
                }
            }
            //  FillerCordWt = FillerCordWt + addCM;
            //if (comboFillerCordTop.SelectedIndex == 1) // Single
            //    FillerCordWt = (FillerCordWt * FillerCordGSM);

            //else if (comboFillerCordTop.SelectedIndex == 2) //Double
            //    FillerCordWt = (FillerCordWt * 2 * FillerCordGSM);
            FillerCordWt = (FillerCordWt * FillerCordGSM);
            FillerCordMtr = FillerCordWt / (FillerCordGSM * 100);
            return FillerCordWt;
        }

        private double FeltWtFormula() //Felt  By Rikin on 14-Feb-2015
        {
            FeltWt = 0;
            int addCM = 0;
            if (_Type == 0) //Internal
            {
                addCM = 10;
            }
            if (checkFeltTop.Checked)
                FeltWt += ((_BagLenght + _BagWidth + addCM) * 2);
            if (checkFeltBottom.Checked)
                if (_BodyIndex1 == 0) //Upanel
                    FeltWt += ((_BagWidth + addCM) * 2);
                else
                    FeltWt += ((_BagLenght + _BagWidth + addCM) * 2);
            if (checkFeltTopSpout.Checked)
                FeltWt += (3.14 * Utility.SafeConvertToDouble(comboSpoutDia.Text)) + 12 + Utility.SafeConvertToDouble(comboSpoutHeight.Text);

            if (checkFeltBottomSpout.Checked)
                FeltWt += (3.14 * Utility.SafeConvertToDouble(comboBoxbottomdia.Text)) + 12 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text);

            if (checkFeltBody.Checked)
            {
                if (_Type == 0) // Internal
                    FeltWt += ((_BagHeight + addCM) * 4);

                else if (_Type == 1) //External
                    FeltWt += (_BagHeight) * 4;

            }

            FeltWt = FeltWt + addCM;
            FeltMtr = Math.Round((FeltWt / 100), 4);
            FeltWt = (FeltWt * 5 * 170) / 10000000;

            //if (checkFeltMfwebbing.Checked) //19.06.2021
            //{
            //    FeltWt = (FeltMtr * 15) / 10000000;//remove 30
            //}
            //else
            //{
            //    FeltWt = (FeltWt * 5 * 170) / 10000000;
            //}


            if (checkFeltUnderTheLoop.Checked && comboLoopConst.SelectedIndex == 2)//UnderTheLoop  added As per dilen ji on 11-Mar-2015
            {
                // LoopCutLenght
                double dropLoop = 0;
                if (checkBoxDropLoop.Checked)
                    dropLoop = Utility.SafeConvertToDouble(textLoopDropLenght.Text);
                FeltUnderTheLoopCutLenght = ((LoopCutLenght - (Utility.SafeConvertToDouble(comboLoopL.Text) + dropLoop) * 2) / 2) + 5;
                FeltUnderTheLoopFabricSize = Utility.SafeConvertToDouble(comboLoopW.Text) + 3;
                FeltUnderTheLoopWt = FeltUnderTheLoopCutLenght * 8;
                FeltUnderTheLoopMtr = Math.Round((FeltUnderTheLoopWt / 100), 4);
                FeltUnderTheLoopWt = (FeltUnderTheLoopWt * FeltUnderTheLoopFabricSize * 170) / 10000000;

            }


            return FeltWt;
        }

        private double MFWebbingWtFormula() //MF Web  By raj on 03-Sep-2021
        {
            MFWebMtr = 0;
            MFWebWt = 0;
            int addCM = 0;
            if (_Type == 0) //Internal
            {
                addCM = 10;
            }
            if (checkMFWebTop.Checked)
                MFWebWt += ((_BagLenght + _BagWidth + addCM) * 2);
            if (checkMFWebBottom.Checked)
            {
                if (_BodyIndex1 == 0) //Upanel
                    MFWebWt += ((_BagWidth + addCM) * 2);
                else
                    MFWebWt += ((_BagLenght + _BagWidth + addCM) * 2);
            }
            if (checkMFWebTopSpout.Checked)
                MFWebWt += (3.14 * Utility.SafeConvertToDouble(comboSpoutDia.Text)) + 12 + Utility.SafeConvertToDouble(comboSpoutHeight.Text);

            if (checkMFWebBottomSpout.Checked)
                MFWebWt += (3.14 * Utility.SafeConvertToDouble(comboBoxbottomdia.Text)) + 12 + Utility.SafeConvertToDouble(comboBoxbottomheight.Text);


            if (checkMFWebBody.Checked)
            {
                if (_Type == 0) // Internal
                    MFWebWt += ((_BagHeight + addCM) * 4);
                else if (_Type == 1) //External
                    MFWebWt += (_BagHeight) * 4;
            }
            if (_BodyIndex1 == 2 && checkMFBuffle.Checked) // Buffle
            {
                if (_Type == 0) // Internal
                {
                    MFWebWt += (_BagHeight + 5) * 8;
                }
                else if (_Type == 1) //External
                {
                    MFWebWt += (_BagHeight) * 8;
                }
            }
            //if (checkMFWebBody.Checked)
            //{
            //    if (_Type == 0) // Internal
            //        MFWebWt += ((_BagHeight + addCM) * 4);

            //    else if (_Type == 1) //External
            //        MFWebWt += (_BagHeight) * 4;
            //}

            //MFWebWt = MFWebWt + addCM;
            MFWebWt = (MFWebWt * 15);
            MFWebMtr = Math.Round((MFWebWt / (15 * 100)), 4);


            return MFWebWt;
        }


        private void btnPrint_Click(object sender, EventArgs e)
        {
            ClearVariables();
            print("temp");
        }

        /// <summary>
        /// Value stored variable
        ///  Created By Rikin on 14-Feb-2015
        /// </summary>
        public void SetValue()
        {
            try
            {

                if (textBodyL.Text.Length == 0)
                    MessageBox.Show("Please Enter value in Body Lenght");
                else if (textBodyW.Text.Length == 0)
                    MessageBox.Show("Please Enter value in Body Width");
                else if (textBodyH.Text.Length == 0)
                    MessageBox.Show("Please Enter value in Body Height");
                else if (textQty.Text.Length == 0)
                    MessageBox.Show("Please Enter value in Qty");
                else
                {
                    _BagHeight = Utility.SafeConvertToDouble(textBodyH.Text);
                    _BagWidth = Utility.SafeConvertToDouble(textBodyW.Text);
                    _BagLenght = Utility.SafeConvertToDouble(textBodyL.Text);
                    _BagQty = Utility.SafeConvertToDouble(textQty.Text);
                    _BodyIndex1 = comboBody1.SelectedIndex;
                    _Type = comboType.SelectedIndex;
                    _BagGSM = Utility.SafeConvertToDouble(comboBodyGSM.Text);
                    if (checkBoxLam.Checked)
                    {
                        if (comboBodyLamiGSM.Text.Contains("+"))
                        {
                            string[] s = comboBodyLamiGSM.Text.Split('+');
                            int lam = 0;
                            for (int i = 0; i < s.Length; i++)
                                lam += Convert.ToInt32(s[i].ToString());
                            _BagLamiGSM = lam;
                        }
                        else
                            _BagLamiGSM = Utility.SafeConvertToDouble(comboBodyLamiGSM.Text);
                    }
                    else
                        _BagLamiGSM = 0;
                    // _BagGSM + _BagLamiGSM

                    _BagSideGSM = Utility.SafeConvertToDouble(comboSideGSM.Text);
                    //_BagSideGSM + _BagSideLamiGSM

                    if (checkSideLami.Checked && checkSide.Checked)
                    {
                        if (checkSideLami.Text.Contains("+"))
                        {
                            string[] s = checkSideLami.Text.Split('+');
                            int lam = 0;
                            for (int i = 0; i < s.Length; i++)
                                lam += Convert.ToInt32(s[i].ToString());
                            _BagSideLamiGSM = lam;
                        }
                        else
                            _BagSideLamiGSM = Utility.SafeConvertToDouble(comboSideLamiGSM.Text);
                    }
                    else
                        _BagSideLamiGSM = 0;

                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.ToString()); }


        }
        /// <summary>
        /// Get Total Mtr of passed Cut length
        /// By Rikin on 17-Mar-2015
        /// </summary>
        /// <param name="CutLength">Cut Length which need to get total mtr</param>
        /// <returns></returns>
        private double setTtotalMtr(double CutLength, int param)
        {
            if (param == 1)
              //  return Math.Round(((CutLength / 100) * _BagQty * param) + .1 * ((CutLength / 100) * _BagQty * param), 4);
                return Math.Round(((CutLength / 100) * _BagQty * param) , 4);
            else if (param == 2)
                //return Math.Round(((CutLength / 100) * _BagQty * param) + .1 * ((CutLength / 100) * _BagQty * param), 4);
                return Math.Round(((CutLength / 100) * _BagQty * param), 4);
            else if (param == 4)
                //return Math.Round(((CutLength / 100) * _BagQty * param) + .1 * ((CutLength / 100) * _BagQty * param), 4);
                return Math.Round(((CutLength / 100) * _BagQty * param ), 4);
            else return 0;

            //  BodyTotalMtr = BodyTotalMtr, 4);

        }
        /// <summary>
        /// Calculaate all the formulas for BOM
        /// Created By Rikin on 11-Feb-2015
        /// </summary>
        /// <returns> Boolen</returns>
        private bool Calculation()
        {
            try
            {

                SetValue();
                BodyWtFormula();
                BodyWt = BodyWt / 10000000;
                BodyWt = Math.Round(BodyWt, 4);
                if ((_BodyIndex1 == 2 && (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 6 || comboBuffleType.SelectedIndex == 1))
                         || _BodyIndex1 == 3)
                {
                    if (textBodyL.Text == textBodyW.Text)
                        BodyTotalMtr = setTtotalMtr(BodyCutLenght, 4);
                    else
                        BodyTotalMtr = setTtotalMtr(BodyCutLenght, 2);

                    if (textBodyNo.Text != "")
                        BodyTotalMtr = BodyTotalMtr * Convert.ToInt32(textBodyNo.Text);
                }
                else
                {
                    BodyTotalMtr = setTtotalMtr(BodyCutLenght, 1);
                    if (textBodyNo.Text != "")
                        BodyTotalMtr = BodyTotalMtr * Convert.ToInt32(textBodyNo.Text);
                }
                if (checkSide.Checked)
                {
                    SideWtFormula();
                    if (_BodyIndex1 == 1) // circular
                        SideWt = BodyWt;
                    else
                        SideWt = SideWt / 10000000;

                    SideWt = Math.Round(SideWt, 4);
                    if (_BodyIndex1 == 0) //UPanel 
                        SideTotalMtr = setTtotalMtr(SideCutLenght, 2);
                    else if (_BodyIndex1 == 3 || _BodyIndex1 == 4) // 4 Panel,Tube + Corner
                    {
                        if (textBodyL.Text == textBodyW.Text)
                            SideTotalMtr = setTtotalMtr(SideCutLenght, 4);
                        else
                            SideTotalMtr = setTtotalMtr(SideCutLenght, 2);
                    }
                    else if (_BodyIndex1 == 2) // Buffle
                    {
                        if (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 6 || comboBuffleType.SelectedIndex == 1)
                            SideTotalMtr = setTtotalMtr(SideCutLenght, 2);
                        else
                            SideTotalMtr = setTtotalMtr(SideCutLenght, 4);

                    }
                    else if (_BodyIndex1 == 12) // Double Layer Tunnel Lift Loop Bag
                        SideTotalMtr = setTtotalMtr(SideCutLenght, 2);

                    else
                        SideTotalMtr = setTtotalMtr(SideCutLenght, 1);
                }

                if (checkBoxTop.Checked)
                {
                    TopWtFormula();
                    FSWtFormula();

                    TopWt = TopWt / 10000000;
                    TopWt = Math.Round(TopWt, 4);
                    TopTotalMtr = setTtotalMtr(TopCutLenght, 1);
                    FSWt = FSWt / 10000000;
                    FSWt = Math.Round(FSWt, 4);
                    FSTotalMtr = ((FSCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textFSNo.Text));
                         // + .1 * ((FSCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textFSNo.Text));
                    FSTotalMtr = Math.Round(FSTotalMtr, 4);


                }

                if (checkbottom.Checked)
                {
                    BaseWtFormula();
                    DSWtFormula();

                    BaseWt = BaseWt / 10000000;
                    BaseWt = Math.Round(BaseWt, 4);
                    BaseTotalMtr = setTtotalMtr(BaseCutLenght, 1);

                    DSWt = DSWt / 10000000;
                    DSWt = Math.Round(DSWt, 4);
                    DSTotalMtr = ((DSCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textDSNo.Text));
                         // + .1 * ((DSCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textDSNo.Text));
                    DSTotalMtr = Math.Round(DSTotalMtr, 4);

                }
                if (checkBottom1.Checked)
                {
                    //BaseWtFormula();
                    DSWtFormula1();

                    //BaseWt = BaseWt / 10000000;
                    //BaseWt = Math.Round(BaseWt, 4);
                    //BaseTotalMtr = setTtotalMtr(BaseCutLenght, 1);

                    DSWt1 = DSWt1 / 10000000;
                    DSWt1 = Math.Round(DSWt1, 4);
                    DSTotalMtr1 = ((DSCutLenght1 / 100) * _BagQty * Utility.SafeConvertToDouble(textDSNo1.Text));
                         // + .1 * ((DSCutLenght1 / 100) * _BagQty * Utility.SafeConvertToDouble(textDSNo1.Text));
                    DSTotalMtr1 = Math.Round(DSTotalMtr1, 4);

                }

                if (checkBottom2.Checked)
                {
                    //    BaseWtFormula();
                    DSWtFormula2();

                    //BaseWt = BaseWt / 10000000;
                    //BaseWt = Math.Round(BaseWt, 4);
                    //BaseTotalMtr = setTtotalMtr(BaseCutLenght, 1);

                    DSWt2 = DSWt2 / 10000000;
                    DSWt2 = Math.Round(DSWt2, 4);
                    DSTotalMtr2 = ((DSCutLenght2 / 100) * _BagQty * Utility.SafeConvertToDouble(textDSNo2.Text));
                          //+ .1 * ((DSCutLenght2 / 100) * _BagQty * Utility.SafeConvertToDouble(textDSNo2.Text));
                    DSTotalMtr2 = Math.Round(DSTotalMtr2, 4);

                }


                if (checkBoxlabel.Checked)
                {
                    LabelWtFormula();

                    LabelWt = LabelWt / 10000000;
                    LabelWt = Math.Round(LabelWt, 4);
                    LabelTotalMtr = setTtotalMtr(LabelCutLenght, 1);

                }
                if (checkBoxdocpouch.Checked)
                {
                    DocWtFormula();
                    DocWt = DocWt / 10000000;
                    DocWt = Math.Round(DocWt, 4);
                    DocTotalMtr = setTtotalMtr(DocCutLenght, 1);
                }
                if (checkdocpouch1.Checked)
                {
                    Doc1WtFormula();
                    Doc1Wt = Doc1Wt / 10000000;
                    Doc1Wt = Math.Round(Doc1Wt, 4);
                    Doc1TotalMtr = setTtotalMtr(Doc1CutLenght, 1);
                }

                if (checkdocpouch2.Checked)
                {
                    Doc2WtFormula();
                    Doc2Wt = Doc2Wt / 10000000;
                    Doc2Wt = Math.Round(Doc2Wt, 4);
                    Doc2TotalMtr = setTtotalMtr(Doc2CutLenght, 1);
                }

                if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 6
                    || comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8 || comboTopType.SelectedIndex == 9)
                {
                    DuffleWtFormula();
                    DuffleWt = DuffleWt / 10000000;
                    DuffleWt = Math.Round(DuffleWt, 4);
                    DuffleTotalMtr = setTtotalMtr(DuffleCutLenght, 1);
                }
                if (comboBoxbottomtype.SelectedIndex == 8)
                {
                    BottomDuffleWtFormula();

                    BottomDuffleWt = BottomDuffleWt / 10000000;
                    BottomDuffleWt = Math.Round(BottomDuffleWt, 4);
                    BottomDuffleTotalMtr = setTtotalMtr(BottomDuffleCutLenght, 1);
                }
                if (checkSpoutTie.Checked)
                {
                    FSTieFormula();

                    FSTieWt = FSTieWt / 100000;
                    FSTieWt = Math.Round(FSTieWt, 4);
                    FSTieTotalMtr = ((FSTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textTopSpoutTieNo.Text));
                         // + .1 * ((FSTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textTopSpoutTieNo.Text));
                    FSTieTotalMtr = Math.Round(FSTieTotalMtr, 4);

                }

                if (checkIRISTie.Checked) //29/09/2021
                {
                    FSIRISTieFormula();
                    FSIRISTieWt = FSIRISTieWt / 100000;
                    FSIRISTieWt = Math.Round(FSIRISTieWt, 4);
                    FSIRISTieTotalMtr = ((FSIRISTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textTopSpoutTieIRISNo.Text));
                         // + .1 * ((FSIRISTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textTopSpoutTieIRISNo.Text));
                    FSIRISTieTotalMtr = Math.Round(FSIRISTieTotalMtr, 4);
                }

                if (checkBottomSpoutTie.Checked)
                {
                    DSTieFormula();
                    DSTieWt = DSTieWt / 100000;
                    DSTieWt = Math.Round(DSTieWt, 4);
                    DSTieTotalMtr = ((DSTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textBottomSpoutTieNo.Text));
                          //+ .1 * ((DSTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textBottomSpoutTieNo.Text));
                    DSTieTotalMtr = Math.Round(DSTieTotalMtr, 4);
                }

                if (checkBottomspoutiristie.Checked)
                {
                    DSIRISTieFormula();
                    DSIRISTieWt = DSIRISTieWt / 100000;
                    DSIRISTieWt = Math.Round(DSIRISTieWt, 4);
                    DSIRISTieTotalMtr = ((DSIRISTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textBottomSpoutTieIRISNo.Text));
                          //+ .1 * ((DSIRISTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textBottomSpoutTieIRISNo.Text));
                    DSIRISTieTotalMtr = Math.Round(DSIRISTieTotalMtr, 4);
                }

                if (checkBottomSpoutTie1.Checked)
                {
                    DSTieFormula1();
                    DSTieWt1 = DSTieWt1 / 100000;
                    DSTieWt1 = Math.Round(DSTieWt1, 4);
                    DSTieTotalMtr1 = ((DSTieCutLenght1 / 100) * _BagQty * Utility.SafeConvertToDouble(textBottomSpoutTieNo.Text));
                          //+ .1 * ((DSTieCutLenght1 / 100) * _BagQty * Utility.SafeConvertToDouble(textBottomSpoutTieNo.Text));
                    DSTieTotalMtr1 = Math.Round(DSTieTotalMtr1, 4);
                }

                if (checkBoxLoop.Checked)
                {
                    LoopWtFormula();
                    LoopWt = LoopWt / 100000;
                    LoopWt = Math.Round(LoopWt, 4);
                    LoopTotalMtr = ((LoopCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textLoopNo.Text));
                         // + .1 * ((LoopCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble(textLoopNo.Text));
                    LoopTotalMtr = Math.Round(LoopTotalMtr, 4);
                }
                #region 17.06.2021
                if (chkFabricPatch.Checked)
                {
                    FabricPatchWtFormula(); //17.06.2021
                    FabricPatchWt = Math.Round(FabricPatchWt / 10000000, 4);
                    FabricPatcTotalMtr = ((FabricPatchCutLength / 100) * _BagQty * Utility.SafeConvertToDouble(textLoopNo.Text));
                         //+ .1 * ((FabricPatchCutLength / 100) * _BagQty * Utility.SafeConvertToDouble(textLoopNo.Text));
                    FabricPatcTotalMtr = Math.Round(FabricPatcTotalMtr, 4);
                }
                #endregion
                if (checkBoxliner.Checked)
                {
                    LinerWtFormula();
                    if (checkBoxlinerBuffle.Checked)
                    {
                        LinerBuffleWt = Math.Round(LinerBuffleWt / 10000000, 4);
                        LinerBuffleTotalMtr = setTtotalMtr(LinerCutLenghtBuffle, 1) * 4;
                    }
                    LinerWt = LinerWt / 10000000;
                    LinerWt = Math.Round(LinerWt, 4);
                    LinerTotalMtr = setTtotalMtr(LinerCutLenght, 1);
                }

                ThreadWtFormula();

                if (checkSpoutRope.Checked)
                {
                    TopSpoutRopeWtFormula();
                    TopSpoutRopeWt = TopSpoutRopeWt / 100000;
                    TopSpoutRopeWt = Math.Round(TopSpoutRopeWt, 4);
                    TopSpoutRopeTotalMtr = setTtotalMtr(TopSpoutRopeCutLenght, 1);

                    TopPetalWT = Math.Round((TopPetalWT / 10000000), 4);
                    TopPetalTotalMtr = ((TopPetalCutLength / 100) * _BagQty);
                }

                if (checkTopTie.Checked)
                {
                    TopTieFormula();

                    TopTieWt = TopTieWt / 100000;
                    TopTieWt = Math.Round(TopTieWt, 4);
                    TopTieTotalMtr = setTtotalMtr(TopTieCutLenght, 1);
                }

                if (checkBottomTie.Checked)
                {
                    BottomTieFormula();

                    BottomTieWt = BottomTieWt / 100000;
                    BottomTieWt = Math.Round(BottomTieWt, 4);
                    BottomTieTotalMtr = setTtotalMtr(BottomTieCutLenght, 1);
                }
                if (checkBottomloop.Checked)
                {
                    BottomLoopFormula();

                    BottomLoopWt = BottomLoopWt / 100000;
                    BottomLoopWt = Math.Round(BottomLoopWt, 4);
                    BottomLoopTotalMtr = setTtotalMtr(BottomLoopLenght, 1);
                }


                if (checkBottomSpoutRope.Checked || checkBottomspoutirisrope.Checked)
                {
                    BottomSpoutRopeWtFormula();

                    BottomSpoutRopeWt = BottomSpoutRopeWt / 100000;
                    BottomSpoutRopeWt = Math.Round(BottomSpoutRopeWt, 4);
                    BottomSpoutRopeTotalMtr = setTtotalMtr(BottomSpoutRopeCutLenght, 1);
                    PetalWT = Math.Round((PetalWT / 10000000), 4);
                    PetalTotalMtr = ((PetalCutLength / 100) * _BagQty);
                }



                if (checkBottomSpoutRope2.Checked)
                {
                    BottomSpoutRopeWtFormula2();

                    BottomSpoutRopeWt2 = BottomSpoutRopeWt2 / 100000;
                    BottomSpoutRopeWt2 = Math.Round(BottomSpoutRopeWt2, 4);
                    BottomSpoutRopeTotalMtr2 = setTtotalMtr(BottomSpoutRopeCutLenght2, 1);
                    //PetalWT = Math.Round((PetalWT / 10000000), 4);
                    //PetalTotalMtr = ((PetalCutLength / 100) * _BagQty);
                }

                if (checkBottomSpoutRope1.Checked)
                {
                    BottomSpoutRopeWtFormula1();

                    BottomSpoutRopeWt1 = BottomSpoutRopeWt1 / 100000;
                    BottomSpoutRopeWt1 = Math.Round(BottomSpoutRopeWt1, 4);
                    BottomSpoutRopeTotalMtr1 = setTtotalMtr(BottomSpoutRopeCutLenght1, 1);
                    //PetalWT = Math.Round((PetalWT / 10000000), 4);
                    //PetalTotalMtr = ((PetalCutLength / 100) * _BagQty);
                }


                if (checkTopFlapHook.Checked)
                {
                    TopHookFormula();

                    TopHookWt = TopHookWt / 100000;
                    TopHookWt = Math.Round(TopHookWt, 4);
                    TopHookTotalMtr = setTtotalMtr(TopHookCutLenght, 1);
                }

                if (checkBottomFlapHook.Checked)
                {
                    BottomHookFormula();

                    BottomHookWt = BottomHookWt / 10000000;
                    BottomHookWt = Math.Round(BottomHookWt, 4);
                    BottomHookTotalMtr = setTtotalMtr(BottomHookCutLenght, 1);
                }

                if (checktopflap.Checked)
                {
                    TopFlapWtFormula();

                    TopFlapWt = TopFlapWt / 10000000;
                    TopFlapWt = Math.Round(TopFlapWt, 4);
                    TopFlapTotalMtr = setTtotalMtr(TopFlapCutLenght, 1);
                }

                if (checkBottomflap.Checked)
                {
                    BottomFlapWtFormula();

                    BottomFlapWt = BottomFlapWt / 10000000;
                    BottomFlapWt = Math.Round(BottomFlapWt, 4);
                    BottomFlapTotalMtr = setTtotalMtr(BottomFlapCutLenght, 1);
                }
                if (checkBottomRope.Checked)
                {
                    BottomRopeWtFormula();
                    BottomRopeWt = BottomRopeWt / 100000;
                    BottomRopeWt = Math.Round(BottomRopeWt, 4);
                    BottomRopeTotalMtr = setTtotalMtr(BottomRopeCutLenght, 1);
                }
                if (checkTopRope.Checked)
                {
                    TopRopeWtFormula();
                    TopRopeWt = TopRopeWt / 100000;
                    TopRopeWt = Math.Round(TopRopeWt, 4);
                    TopRopeTotalMtr = setTtotalMtr(TopRopeCutLenght, 1);
                }
                if (checkFillerCord.Checked)
                {
                    FillerCordWtFormula();

                    FillerCordWt = FillerCordWt / 100000;
                    FillerCordWt = Math.Round(FillerCordWt, 4);
                }
                if (checkFelt.Checked)
                {
                    FeltWtFormula();
                }
                if (checkFeltMfwebbing.Checked)
                {
                    MFWebbingWtFormula();
                    MFWebWt = MFWebWt / 100000;
                    MFWebWt = Math.Round(MFWebWt, 4);
                }
                if (comboLoopConst.SelectedIndex == 3 && (_BodyIndex1 == 0 || _BodyIndex1 == 1
                     || _BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4))
                {
                    FullLoopWt = FullLoopWt / 100000; //Change by 1000000
                    FullLoopWt = Math.Round(FullLoopWt, 4);
                    FullLoopTotalMtr = setTtotalMtr(FullLoopCutLenght, 1);
                }
                if (checkBoxTopBand.Checked)
                    TopBandWtFormula();
                #region 18.06.2021
                if (checkBoxTopBellyBand1.Checked)
                    TopBandBellyBand1WtFormula();
                if (checkBoxTopBellyBand2.Checked)
                    TopBandBellyBand2WtFormula();
                if (checkBoxTopBottomBand.Checked)
                    TopBttomBandWtFormula();
                #endregion
                if (checkLoopCover.Checked)
                    LoopCoverWtFormula();
                if (checkStevedorecover.Checked)
                    SteveCoverWtFormula();

                if (_BodyIndex1 == 2) //Buffle
                {
                    BuffleWtFormula();
                    if (comboBuType.SelectedIndex == 0 || comboBuType.SelectedIndex == 2 || comboBuType.SelectedIndex == 3)
                        BuffleWt = BuffleWt / 10000000;
                    else
                        BuffleWt = BuffleWt / 1000;

                    BuffleWt = Math.Round(BuffleWt, 4);
                    BuffleTotalMtr = setTtotalMtr(BuffleCutLenght, 4);
                }

                if (checkInnerBox.Checked)
                    InnerBoxWtFormula();
                if (checkStevdore.Checked)
                    StevedoreWtFormula();
                if (checkLoopProtector.Checked)
                    LoopProtectorWtFormula();
                //if (checkBottomloop.Checked)
                //    BottomLoopFormula();

                if (checkInnerSkin.Checked)
                {
                    InnerSkinWtFormula();
                    InnerSkinWt = InnerSkinWt / 10000000;
                    InnerSkinWt = Math.Round(InnerSkinWt, 4);
                    if ((_BodyIndex1 == 2 && (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 6 || comboBuffleType.SelectedIndex == 1))
                             || _BodyIndex1 == 3)
                    {
                        if (textBodyL.Text == textBodyW.Text)
                            InnerSkinTotalMtr = setTtotalMtr(InnerSkinCutLenght, 4);
                        else
                            InnerSkinTotalMtr = setTtotalMtr(InnerSkinCutLenght, 2);
                    }
                    else
                        InnerSkinTotalMtr = setTtotalMtr(InnerSkinCutLenght, 1);
                }

                if (checkInnerTop.Checked)
                {
                    InnerTopWtFormula();
                    InnerTopWt = InnerTopWt / 10000000;
                    InnerTopWt = Math.Round(InnerTopWt, 4);
                    InnerTopTotalMtr = setTtotalMtr(InnerTopCutLenght, 1);
                }

                if (checkInnerBottom.Checked)
                {
                    InnerBottomWtFormula();

                    InnerBottomWt = InnerBottomWt / 10000000;
                    InnerBottomWt = Math.Round(InnerBottomWt, 4);
                    InnerBottomTotalMtr = setTtotalMtr(InnerBottomCutLenght, 1);
                }
                if (checkAncerieLoop.Checked)
                    AncerieWtFormula();

                return false;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return true;
                //IsError = true;
            }
        }

        private void addDataRow(string head, string GSM, string Lam, string Color, string FabSize, string cutSize, string TotalMtr, string totalKG, string Remarks, string SrNo, ref DataSet ds,string category)
        {

            DataRow drs = ds.Tables[0].NewRow();
            if (head == "Thread")
            {
                drs["Heading"] = head.ToString();
                drs["GSM"] = GSM.ToString();
                drs["Lami"] = Lam.ToString();
                drs["Color"] = Color.ToString();
                drs["FabricSize"] = FabSize.ToString();
                drs["CutSize"] = Math.Round(Utility.SafeConvertToDouble(cutSize), 0, MidpointRounding.AwayFromZero);
                drs["TotalMtr"] = TotalMtr.ToString();
                drs["HeadTotalKG"] = totalKG.ToString();
                drs["Remarks"] = Remarks.ToString();
                drs["PONo"] = textFilePONo.Text;
                drs["SrNo"] = SrNo;
                drs["gpm"] = category;
                ds.Tables[0].Rows.Add(drs);
            }
            else
            {
                double lam = 0;
                if (GSM == "")
                    lam = 0;
                else
                {
                    string[] s = GSM.Split('+');

                    for (int i = 0; i < s.Length; i++)
                    {
                        if(s[i].Trim() != "")
                         lam += Convert.ToDouble(s[i].Trim());
                    }
                }
                if (lam > 0)
                {
                    drs["Heading"] = head.ToString();
                    drs["GSM"] = GSM.ToString();
                    drs["Lami"] = Lam.ToString();
                    drs["Color"] = Color.ToString();
                    drs["FabricSize"] = FabSize.ToString();
                    drs["CutSize"] = Math.Round(Utility.SafeConvertToDouble(cutSize), 0, MidpointRounding.AwayFromZero);
                    drs["TotalMtr"] = TotalMtr.ToString();
                    drs["HeadTotalKG"] = totalKG.ToString();
                    drs["Remarks"] = Remarks.ToString();
                    drs["PONo"] = textFilePONo.Text;
                    drs["SrNo"] = SrNo;
                    drs["gpm"] = category;
                    ds.Tables[0].Rows.Add(drs);
                }
            }
        }


        void print(string SrNo)
        {
            bool IsError = false;
            TotalKg = 0;
            if (Database.OpenConnection(Utility.ProductionConnectionString))
            {

                if (textBodyNo.Text == "")
                    textBodyNo.Text = "1";
                if (textFillHt.Text == "")
                    textFillHt.Text = "0";
                if (textConicaltop.Text == "")
                    textConicaltop.Text = "0";
                if (textDocNo.Text == "")
                    textDocNo.Text = "1";
                if (textBottomNo.Text == "")
                    textBottomNo.Text = "1";
                if (textSideNo.Text == "")
                    textSideNo.Text = "1";
                if (checkBoxDropLoop.Checked)
                {
                    if (textLoopDropLenght.Text == "")
                        textLoopDropLenght.Text = "0";

                }

                IsTemp = false;
                IsError = Calculation();
                if (!IsError)
                {
                    try
                    {
                        int count = 0, x = 0;
                        #region bom
                        if (IsupdateMode && btnupdateclick)
                        {
                            Database.BeginTransaction();
                            Database.GetExecuteNonQueryCommand("insert into BOM1_delete select * from bom1 where FilePONo = '" + textFilePONo.Text + "'");
                            Database.GetExecuteNonQueryCommand("insert into BOM_delete select * from bom where PONo = '" + textFilePONo.Text + "'"); //heading,gsm,lami,color,fabricsize,cutsize,totalmtr,totalkg,partyname,pono,srno,modifydate,remarks,gpm,warpdenier,warptape,warpgsm,warpkg,weftdenier,wefttape,weftgsm,weftkg
                            Database.GetExecuteNonQueryCommand("insert into BOM2_delete select * from bom2 where PONo = '" + textFilePONo.Text + "'");
                            Database.GetExecuteNonQueryCommand("insert into BOM3_delete Select * from bom3 where PONo = '" + textFilePONo.Text + "'");
                            Database.GetExecuteNonQueryCommand("delete from BOM1 where FilePONo = '" + textFilePONo.Text + "'");
                            Database.GetExecuteNonQueryCommand("delete from BOM2 where PONo = '" + textFilePONo.Text + "'");
                            Database.GetExecuteNonQueryCommand("delete from BOM3 where PONo = '" + textFilePONo.Text + "'");
                            Database.GetExecuteNonQueryCommand("delete from BOM where PONo = '" + textFilePONo.Text + "'");
                        }
                        SqlDataAdapter myadpter1 = Database.GetAdapterCommand("Select   dbo.BOM.Heading  ,dbo.BOM.GSM , dbo.BOM.Lami , dbo.BOM.Color , " +
                         " dbo.BOM.FabricSize ,  dbo.BOM.CutSize   as CutSize, dbo.BOM.TotalMtr , dbo.BOM.TotalKg as HeadTotalKG ,BOM.Remarks,dbo.BOM.PONo,dbo.BOM.SrNo,dbo.BOM.GPM  from BOM WITH(nolock) where 1=2");
                        DataSet dataset2 = new DataSet();
                        myadpter1.Fill(dataset2);
                        SqlCommandBuilder cmd = new SqlCommandBuilder();
                        //Body Formula by un commented By Rikin on 12-Mar-2015 as Same as Non builder bag
                        #region Body
                        if (comboBody1.Text.Contains("4 Panel") && (textBodyL.Text == textBodyW.Text))
                        {
                            //Change as 29.09.2021 for 4 Panel it will not calculate Body Weight
                            //addDataRow("Body", ((checkBoxLam.Checked) ? comboBodyGSM.Text + " + " + comboBodyLamiGSM.Text : comboBodyGSM.Text),
                            //  ((checkBoxLam.Checked) ? "Laminated" : "UnLaminated"), comboBodyColor.Text, BodyFabricSize.ToString(),
                            //  BodyCutLenght.ToString(), BodyTotalMtr.ToString(), BodyWt.ToString(), textBodyRemarks.Text, SrNo, ref dataset2);

                            TotalKg += BodyWt;
                        }
                        else
                        {
                            addDataRow("Body", ((checkBoxLam.Checked) ? comboBodyGSM.Text + " + " + comboBodyLamiGSM.Text : comboBodyGSM.Text),
                                ((checkBoxLam.Checked) ? "Laminated" : "UnLaminated"), comboBodyColor.Text, BodyFabricSize.ToString(),
                                BodyCutLenght.ToString(), BodyTotalMtr.ToString(), BodyWt.ToString(), textBodyRemarks.Text, SrNo, ref dataset2 ,"");

                            TotalKg += BodyWt;
                        }

                        // Side Formula
                        if (checkSide.Checked)
                        {
                            //string remarks = "";
                            //if (_BodyIndex1 == 2)
                            //{
                            //    if (comboType.SelectedIndex == 0 && comboBody2.SelectedIndex == 7) // Internal
                            //    {
                            //        double x1 = Math.Round(_BagWidth / 3, 1);
                            //        double y = x1 + 2;
                            //        remarks = "Buffle Coding " + y.ToString() + "," + x1.ToString() + "," + y.ToString();
                            //    }
                            //    else if (comboType.SelectedIndex == 0 && comboBody2.SelectedIndex != 7) // Internal
                            //    {
                            //        double x1 = Math.Round(_BagWidth / 3, 1);
                            //        double y = x1 + 5.5;
                            //        remarks = "Buffle Coding " + y.ToString() + "," + x1.ToString() + "," + y.ToString();
                            //    }
                            //    //Buffle Coding 35.5,30,35.5 nonsuzler
                            //}

                            addDataRow("Side", ((checkSideLami.Checked) ? comboSideGSM.Text + " + " + comboSideLamiGSM.Text : comboSideGSM.Text),
                               ((checkSideLami.Checked) ? "Laminated" : "UnLaminated"), comboSideColor.Text, SideFabricSize.ToString(),
                               SideCutLenght.ToString(), SideTotalMtr.ToString(), SideWt.ToString(), textSideRemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += SideWt;
                        }
                        #endregion
                        #region Top
                        // Top Formula
                        if (checkBoxTop.Checked && comboTopType.SelectedIndex != 0 && comboTopType.SelectedIndex != 2 && comboTopType.SelectedIndex != 6
                            && comboTopType.SelectedIndex != 9)
                        {
                            addDataRow("Top", ((checkBoxTopLam.Checked) ? comboBoxTopGSM.Text + " + " + comboTopLamiGSM.Text : comboBoxTopGSM.Text),
                              ((checkBoxTopLam.Checked) ? "Laminated" : "UnLaminated"), comboTopColor.Text, TopFabricSize.ToString(),
                              TopCutLenght.ToString(), TopTotalMtr.ToString(), TopWt.ToString(), textTopRemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += TopWt;
                        }
                        // Top Spout/ FS Spout
                        if ((checkBoxTop.Checked == true) && (comboTopType.SelectedIndex == 1 || comboTopType.SelectedIndex == 3 || comboTopType.SelectedIndex == 4)) //Top Type = Top Spout
                        {
                            addDataRow("Top Spout", ((checkBoxSpoutLam.Checked) ? comboSpoutGSM.Text + " + " + comboSpoutLamiGSM.Text : comboSpoutGSM.Text),
                                ((checkBoxSpoutLam.Checked) ? "Laminated" : "UnLaminated"), comboSpoutColor.Text, FSFabricSize.ToString(),
                                FSCutLenght.ToString(), FSTotalMtr.ToString(), FSWt.ToString(), textTopSpoutRemarks.Text, SrNo, ref dataset2, "");
                            TotalKg += FSWt;

                            if (checkTopHoseSlider.Checked)
                            {
                                double wt = Utility.SafeConvertToDouble(textHoseSliderNo.Text) * .005;
                                addDataRow("FS Hose Slider", "", "", "", comboTopHoseSlider.Text, comboTopHoseSliderCutSize.Text, "0", wt.ToString()
                                    , "No:- " + textHoseSliderNo.Text, SrNo, ref dataset2, "");
                                TotalKg += DSWt;
                            }

                        }
                        if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 6 ||
                            comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8 || comboTopType.SelectedIndex == 9) // Duffle 17.06.2021
                        {
                            string head = "";
                            string grm = "";
                            string lami = "";
                            if (comboTopType.SelectedIndex == 6) //Rikin on 11-Feb-2015
                                head = "Leno";
                            else if (comboTopType.SelectedIndex == 9) //Rikin on 11-Feb-2015
                                head = "Jute Skirt";
                            else if (comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8)
                                head = "Top " + comboTopType.Text;
                            else
                                head = "Top Duffle/Skrit";
                            if (checkBoxTopLam.Checked)
                                grm = comboBoxTopGSM.Text + " + " + comboTopLamiGSM.Text;
                            else
                                grm = comboBoxTopGSM.Text;
                            if (checkBoxTopLam.Checked)
                                lami = "Laminated";
                            else
                                lami = "UnLaminated";

                            addDataRow(head, grm, lami, comboTopColor.Text, DuffleFabricSize.ToString(),
                             DuffleCutLenght.ToString(), DuffleTotalMtr.ToString(), DuffleWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += DuffleWt;
                        }

                        // FSTie
                        if (checkSpoutTie.Checked)
                        {
                            addDataRow("Top Spout Tie", comboSpoutTieGrm.Text, "", comboTopSpoutTieColor.Text, FSTieFabricSize.ToString(),
                                FSTieCutLenght.ToString(), FSTieTotalMtr.ToString(), FSTieWt.ToString(), textTopSpoutTieRemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += FSTieWt;

                        }
                        if (checkIRISTie.Checked)
                        {
                            if (comboSpoutType.SelectedIndex == 4) // iris
                            {

                                double cutlenght = Convert.ToDouble(comboSpoutDia.Text) * 3.14 + 35;
                                double Wt = cutlenght * Utility.SafeConvertToDouble(comboSpoutTieIRISGrm.Text) * Utility.SafeConvertToDouble(textTopSpoutTieIRISNo.Text);

                                Wt = Wt / 100000;

                                addDataRow("IRIS Tie", comboSpoutTieGrm.Text, "", comboTopSpoutTieIRISColor.Text, FSTieFabricSize.ToString(),
                                    cutlenght.ToString(), FSTieTotalMtr.ToString(), Wt.ToString(), textTopSpoutTieRemarks.Text, SrNo, ref dataset2, "");

                                //TotalKg += FSTieWt;
                                TotalKg += Wt;
                            }
                        }

                        if (checktopflap.Checked)
                        {
                            addDataRow("Top Flap", ((checkTopFlapLami.Checked) ? comboBoxtopflapgsm.Text + " + " + comboTopflapLamiGsm.Text : comboBoxtopflapgsm.Text),
                                ((checkTopFlapLami.Checked) ? "Laminated" : "UnLaminated"), comboTopFlapColor.Text, TopFlapFabricSize.ToString(),
                               TopFlapCutLenght.ToString(), TopFlapTotalMtr.ToString(), TopFlapWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += TopFlapWt;

                        }


                        if (checkTopRope.Checked)
                        {
                            addDataRow("Top Rope", comboTopRopeGrms.Text, comboTopRopeTypes.Text, comboTopRopeColor.Text, TopRopeFabricSize.ToString(),
                               TopRopeCutLenght.ToString(), TopRopeTotalMtr.ToString(), TopRopeWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += TopRopeWt;
                        }
                        if (checkSpoutRope.Checked)
                        {
                            string Head = "TopSpout Rope";
                            if (comboSpoutType.SelectedIndex == 2)
                            { Head = "Top Petal Rope"; }

                            addDataRow(Head, comboTopSpoutRopeGrm.Text, "", comboTopSpoutRopeColor.Text, TopSpoutRopeFabricSize.ToString(),
                               TopSpoutRopeCutLenght.ToString(), TopSpoutRopeTotalMtr.ToString(), TopSpoutRopeWt.ToString(), texttopspoutroperemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += TopSpoutRopeWt;

                            if (comboSpoutType.SelectedIndex == 2)
                            {
                                addDataRow("Top Petal Flap", comboTopPetalFlapGSM.Text + " + " + (checkPetalFlapGSMLam.Checked ? comboTopPetalFlapGSMLam.Text : "0"),
                                    ((checkPetalFlapGSMLam.Checked) ? "Laminated" : "UnLaminated"), comboBodyColor.Text, TopPetalSize.ToString(),
                                   TopPetalCutLength.ToString(), TopPetalTotalMtr.ToString(), TopPetalWT.ToString(), texttopspoutroperemarks.Text, SrNo, ref dataset2, "");

                                TotalKg += PetalWT;
                            }

                        }

                        if (checkIRISRope.Checked)
                        {
                            if (comboSpoutType.SelectedIndex == 4) //iris
                            {

                                double cutlenght = Convert.ToDouble(comboSpoutDia.Text) * 3.14 + 35;
                                double Wt = cutlenght * Utility.SafeConvertToDouble(comboTopSpoutRopeGrm.Text) * Utility.SafeConvertToDouble(textTopSpoutRopeNo.Text);
                                Wt = Wt / 100000;

                                addDataRow("IRIS Rope", comboTopSpoutRopeGrm.Text, "", comboTopSpoutRopeColor.Text, TopSpoutRopeFabricSize.ToString(),
                               cutlenght.ToString(), TopSpoutRopeTotalMtr.ToString(), Wt.ToString(), texttopspoutroperemarks.Text, SrNo, ref dataset2, "");

                            }
                        }
                        if (checkTopFlapHook.Checked)
                        {

                            addDataRow("Top Hook", comboTopflapHookGrm.Text, "", comboTopHookColor.Text, TopHookFabricSize.ToString(),
                               TopHookCutLenght.ToString(), TopHookTotalMtr.ToString(), TopHookWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += TopHookWt;
                        }
                        if (checkTopTie.Checked)
                        {
                            addDataRow("Top Tie", comboTopTieGrms.Text, "", comboTopTieColor.Text, TopTieFabricSize.ToString(),
                                TopTieCutLenght.ToString(), TopTieTotalMtr.ToString(), TopTieWt.ToString(), textTopSpoutTieRemarks.Text, SrNo, ref dataset2, "");
                            TotalKg += TopTieWt;
                        }
                        if (checkBoxTopBand.Checked)
                        {
                            addDataRow("Top Band", combotopbandgrm.Text, "", comboTopBandColor.Text, TopBandFabricSize.ToString(),
                             TopBandCutLenght.ToString(), TopBandTotalMtr.ToString(), TopBandWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += TopBandWt;
                        }
                        //18.06.2021
                        if (checkBoxTopBellyBand1.Checked)
                        {
                            addDataRow("Belly Band 1", combotopBellyband1grm.Text, "", comboTopBellyBand1Color.Text, TopBellyBand1FabricSize.ToString(),
                             TopBellyBand1CutLenght.ToString(), TopBellyBand1TotalMtr.ToString(), TopBellyBand1Wt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += TopBellyBand1Wt;
                        }
                        if (checkBoxTopBellyBand2.Checked)
                        {
                            addDataRow("Belly Band 2", combotopBellyband2grm.Text, "", comboTopBellyBand2Color.Text, TopBellyBand2FabricSize.ToString(),
                             TopBellyBand2CutLenght.ToString(), TopBellyBand2TotalMtr.ToString(), TopBellyBand2Wt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += TopBellyBand2Wt;
                        }
                        if (checkBoxTopBottomBand.Checked)
                        {
                            addDataRow("Bottom Band", combotopBottomBandgrm.Text, "", comboTopBottomBandColor.Text, TopBottomBandFabricSize.ToString(),
                             TopBottomBandCutLenght.ToString(), TopBottomBandTotalMtr.ToString(), TopBottomBandWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += TopBottomBandWt;
                        }
                        //18.06.2021

                        #endregion
                        #region Botton
                        // Bottom Formula
                        if (checkbottom.Checked && _BodyIndex1 != 0)
                        {
                            if ((_BodyIndex1 == 6 || _BodyIndex1 == 5) && (comboBoxbottomtype.SelectedIndex == 5))
                            { }
                            else
                            {
                                if (comboBoxbottomtype.SelectedIndex != 8)
                                {
                                    addDataRow("Bottom", ((checkBoxbottomlam.Checked) ? comboBoxbottomgsm.Text + " + " + comboBottomLamiGSM.Text : comboBoxbottomgsm.Text),
                                      ((checkBoxbottomlam.Checked) ? "Laminated" : "UnLaminated"), comboBottomColor.Text, BaseFabricSize.ToString(),
                                      BaseCutLenght.ToString(), BaseTotalMtr.ToString(), BaseWt.ToString(), textBottomrem.Text, SrNo, ref dataset2, "");
                                    TotalKg += BaseWt;
                                }
                            }
                        }
                        //// Bottom Spout
                        if (checkbottom.Checked && (comboBoxbottomsubtype.SelectedIndex == 0 || comboBoxbottomsubtype.SelectedIndex == 1 || comboBoxbottomsubtype.SelectedIndex == 2 || comboBoxbottomsubtype.SelectedIndex == 6))
                        {
                            if (comboBoxbottomtype.SelectedIndex != 8)
                            {
                                addDataRow("Bottom Spout", ((checkBoxbottomlam1.Checked) ? comboBoxbottomgsm1.Text + " + " + comboBoxBottomSubTypeLamiGSM.Text : comboBoxbottomgsm1.Text),
                                     ((checkBoxbottomlam1.Checked) ? "Laminated" : "UnLaminated"), comboBottomSpoutColor.Text, DSFabricSize.ToString(),
                                    DSCutLenght.ToString(), DSTotalMtr.ToString(), DSWt.ToString(), textBottomRemarks.Text, SrNo, ref dataset2, "");

                                TotalKg += DSWt;

                                if (checkBottomhoseslider.Checked)
                                {
                                    double wt = Utility.SafeConvertToDouble(textBottomhosesliderno.Text) * .005;
                                    addDataRow("DS Hose Slider", "", "", "", comboBottomhoseslider.Text, comboBottomhosesliderCutsize.Text,
                                        "0", wt.ToString(), "No: " + textBottomhosesliderno.Text, SrNo, ref dataset2, "");
                                    TotalKg += DSWt;

                                }

                            }
                        }

                        //// Bottom Spout
                        if (checkBottom1.Checked && (comboBoxbottomsubtype1.SelectedIndex == 0 || comboBoxbottomsubtype1.SelectedIndex == 1 || comboBoxbottomsubtype1.SelectedIndex == 2))
                        {

                            addDataRow("Bottom Spout1", ((checkBoxbottomlam3.Checked) ? comboBoxbottomgsm3.Text + " + " + comboBoxBottomSubTypeLamiGSM1.Text : comboBoxbottomgsm3.Text),
                                 ((checkBoxbottomlam3.Checked) ? "Laminated" : "UnLaminated"), comboBottomSpoutColor1.Text, DSFabricSize1.ToString(),
                                DSCutLenght1.ToString(), DSTotalMtr1.ToString(), DSWt1.ToString(), textBottomRemarks1.Text, SrNo, ref dataset2, "");

                            TotalKg += DSWt1;
                        }

                        //// Bottom Spout
                        if (checkBottom2.Checked && (comboBoxbottomsubtype2.SelectedIndex == 0 || comboBoxbottomsubtype2.SelectedIndex == 1 || comboBoxbottomsubtype2.SelectedIndex == 2))
                        {

                            addDataRow("Bottom Spout2", ((checkBoxbottomlam5.Checked) ? comboBoxbottomgsm5.Text + " + " + comboBoxBottomSubTypeLamiGSM2.Text : comboBoxbottomgsm5.Text),
                                 ((checkBoxbottomlam5.Checked) ? "Laminated" : "UnLaminated"), comboBottomSpoutColor2.Text, DSFabricSize2.ToString(),
                                DSCutLenght2.ToString(), DSTotalMtr2.ToString(), DSWt2.ToString(), textBottomRemarks2.Text, SrNo, ref dataset2, "");

                            TotalKg += DSWt2;
                        }
                        // DSTie
                        if (checkBottomSpoutTie.Checked)
                        {
                            addDataRow("Bottom Spout Tie", comboBottomSpoutTieGrm.Text,
                                "", comboBottomSpoutTieColor.Text, DSTieFabricSize.ToString(),
                               DSTieCutLenght.ToString(), DSTieTotalMtr.ToString(), DSTieWt.ToString(), textBottomSpoutTieRemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += DSTieWt;
                        }

                        if (checkBottomspoutiristie.Checked)
                        {
                            //29.09.2021
                            double cutlenght = Convert.ToDouble(comboBoxbottomdia.Text) * 3.14 + 35;
                            double Wt = cutlenght * Utility.SafeConvertToDouble(comboBottomSpoutTieIRISGrm.Text) * Utility.SafeConvertToDouble(textBottomSpoutTieIRISNo.Text);

                            Wt = Wt / 100000;

                            //addDataRow("IRIS Bottom Tie", comboBottomSpoutTieGrm.Text,
                            //    "", comboBottomSpoutTieColor.Text, DSTieFabricSize.ToString(),
                            //   DSTieCutLenght.ToString(), DSTieTotalMtr.ToString(), DSTieWt.ToString(), textBottomSpoutTieRemarks.Text, SrNo, ref dataset2);

                            addDataRow("IRIS Bottom Tie", comboBottomSpoutTieGrm.Text,
                               "", comboBottomSpoutTieIRISColor.Text, DSIRISTieFabricSize.ToString(),
                              cutlenght.ToString(), DSIRISTieTotalMtr.ToString(), Wt.ToString(), textBottomSpoutTieIRISRemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += Wt;
                        }

                        if (checkBottomSpoutTie1.Checked)
                        {
                            addDataRow("Bottom Spout Tie1", comboBottomSpoutTieGrm1.Text,
                                "", comboBottomSpoutTieColor1.Text, DSTieFabricSize1.ToString(),
                               DSTieCutLenght1.ToString(), DSTieTotalMtr1.ToString(), DSTieWt1.ToString(), textBottomSpoutTieRemarks1.Text, SrNo, ref dataset2, "");

                            TotalKg += DSTieWt1;
                        }

                        if (checkBottomSpoutTie2.Checked)
                        {
                            addDataRow("Bottom Spout Tie2", comboBottomSpoutTieGrm2.Text,
                                "", comboBottomSpoutTieColor2.Text, DSTieFabricSize2.ToString(),
                               DSTieCutLenght2.ToString(), DSTieTotalMtr2.ToString(), DSTieWt2.ToString(), textBottomSpoutTieRemarks2.Text, SrNo, ref dataset2, "");

                            TotalKg += DSTieWt2;
                        }
                        if (checkBottomflap.Checked)
                        {
                            addDataRow("Bottom Flap", ((checkBottomFlapLami.Checked) ? comboBottomflapGSM.Text + " + " + comboBottomflapLamiGSM.Text : comboBottomflapGSM.Text),
                                 ((checkBottomFlapLami.Checked) ? "Laminated" : "UnLaminated"), comboBottomFlapColor.Text, BottomFlapFabricSize.ToString(),
                                BottomFlapCutLenght.ToString(), BottomFlapTotalMtr.ToString(), BottomFlapWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += BottomFlapWt;
                        }
                        if (checkBottomRope.Checked)
                        {
                            addDataRow("Bottom Rope", comboBottomRopeGrms.Text, comboBottomRopeTypes.Text, comboBottomRopeColor.Text, BottomRopeFabricSize.ToString(),
                                 BottomRopeCutLenght.ToString(), BottomRopeTotalMtr.ToString(), BottomRopeWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += BottomRopeWt;
                        }
                        if (comboBoxbottomtype.SelectedIndex == 8) //Bottom Duffle
                        {
                            string grm = "";
                            string lami = "";

                            if (checkBoxbottomlam.Checked)
                                grm = comboBoxbottomgsm.Text + " + " + comboBottomLamiGSM.Text;
                            else
                                grm = comboBoxbottomgsm.Text;
                            if (checkBoxbottomlam.Checked)
                                lami = "Laminated";
                            else
                                lami = "UnLaminated";

                            addDataRow("Bottom Duffle/Skrit", grm, lami, "", BottomDuffleFabricSize.ToString(),
                             BottomDuffleCutLenght.ToString(), BottomDuffleTotalMtr.ToString(), BottomDuffleWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += BottomDuffleWt;

                        }

                        if (checkBottomSpoutRope.Checked)
                        {
                            string head = "";
                            if (comboBoxbottomsubtype.SelectedIndex == 1)
                                head = "Petal Rope";
                            else
                                head = "BottomSpout Rope";

                            addDataRow(head, comboBottomSpoutRopeGrm.Text, "", comboBottomRopeColor.Text, BottomSpoutRopeFabricSize.ToString(),
                               BottomSpoutRopeCutLenght.ToString(), BottomSpoutRopeTotalMtr.ToString(), BottomSpoutRopeWt.ToString(), textBottomspoutroperemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += BottomSpoutRopeWt;

                            if (comboBoxbottomsubtype.SelectedIndex == 1)
                                addDataRow("Bottom Petal Flap", comboBottomPetalFlapGSM.Text + " + " + (checkbottomPetalFlapGSM.Checked ? combobottomPetalFlapGSMLam.Text : "0" ),
                                    ((checkbottomPetalFlapGSM.Checked) ? "Laminated" : "UnLaminated"), comboBodyColor.Text, PetalSize.ToString(),
                                   PetalCutLength.ToString(), PetalTotalMtr.ToString(), PetalWT.ToString(), textBottomspoutroperemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += PetalWT;
                        }

                        if (checkBottomspoutirisrope.Checked)
                        {
                            if (comboBoxbottomsubtype.SelectedIndex == 2)
                            {
                                addDataRow("IRIS Bottom Rope", comboBottomSpoutRopeGrm.Text, "", comboBottomRopeColor.Text, BottomSpoutRopeFabricSize.ToString(),
                              BottomSpoutRopeCutLenght.ToString(), BottomSpoutRopeTotalMtr.ToString(), BottomSpoutRopeWt.ToString(), textBottomspoutroperemarks.Text, SrNo, ref dataset2, "");
                            }
                        }


                        if (checkBottomSpoutRope1.Checked)
                        {
                            string head = "";
                            if (comboBoxbottomsubtype1.SelectedIndex == 1)
                                head = "Petal Rope1";
                            else
                                head = "BottomSpout Rope1";

                            addDataRow(head, comboBottomSpoutRopeGrm1.Text, "", comboBottomSpoutRopeColor1.Text, BottomSpoutRopeFabricSize1.ToString(),
                               BottomSpoutRopeCutLenght1.ToString(), BottomSpoutRopeTotalMtr1.ToString(), BottomSpoutRopeWt1.ToString(), textBottomspoutroperemarks1.Text, SrNo, ref dataset2, "");

                            TotalKg += BottomSpoutRopeWt1;

                            //if (comboBoxbottomsubtype.SelectedIndex == 1)
                            //    addDataRow("Petal Flap", comboBodyGSM.Text + " + " + (_BagLamiGSM > 0 ? comboBodyLamiGSM.Text : "25"), "", comboBodyColor.Text, PetalSize.ToString(),
                            //       PetalCutLength.ToString(), PetalTotalMtr.ToString(), PetalWT.ToString(), textBottomspoutroperemarks.Text, SrNo, ref dataset2);

                            //TotalKg += PetalWT;
                        }

                        if (checkBottomSpoutRope2.Checked)
                        {
                            string head = "";
                            if (comboBoxbottomsubtype2.SelectedIndex == 1)
                                head = "Petal Rope2";
                            else
                                head = "BottomSpout Rope2";

                            addDataRow(head, comboBottomSpoutRopeGrm2.Text, "", comboBottomSpoutRopeColor2.Text, BottomSpoutRopeFabricSize2.ToString(),
                               BottomSpoutRopeCutLenght2.ToString(), BottomSpoutRopeTotalMtr2.ToString(), BottomSpoutRopeWt2.ToString(), textBottomspoutroperemarks2.Text, SrNo, ref dataset2, "");

                            TotalKg += BottomSpoutRopeWt2;

                            //if (comboBoxbottomsubtype.SelectedIndex == 1)
                            //    addDataRow("Petal Flap", comboBodyGSM.Text + " + " + (_BagLamiGSM > 0 ? comboBodyLamiGSM.Text : "25"), "", comboBodyColor.Text, PetalSize.ToString(),
                            //       PetalCutLength.ToString(), PetalTotalMtr.ToString(), PetalWT.ToString(), textBottomspoutroperemarks.Text, SrNo, ref dataset2);

                            //TotalKg += PetalWT;
                        }



                        if (checkBottomFlapHook.Checked)
                        {
                            addDataRow("Bottom Hook", comboBottomFlapHookGrm.Text, "", comboBottomHookColor.Text, BottomHookFabricSize.ToString(),
                                BottomHookCutLenght.ToString(), BottomHookTotalMtr.ToString(), BottomHookWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += BottomHookWt;

                        }
                        if (checkBottomTie.Checked)
                        {
                            addDataRow("Bottom Tie", comboBottomTieGrm.Text, "", comboBottomTieColor.Text, BottomTieFabricSize.ToString(),
                              BottomTieCutLenght.ToString(), BottomTieTotalMtr.ToString(), BottomTieWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += BottomTieWt;
                        }

                        #endregion
                        #region Loop
                        if (checkBoxLoop.Checked)
                        {

                            addDataRow("Loop", comboLoopGrm.Text, comboLoopType.Text, comboLoopColor.Text, LoopFabricSize.ToString(),
                             LoopCutLenght.ToString(), LoopTotalMtr.ToString(), LoopWt.ToString(), textLoopRemarks.Text, SrNo, ref dataset2,"");

                            TotalKg += LoopWt;
                        }
                        #region 17.06.2021
                        if (chkFabricPatch.Checked)
                        {
                            addDataRow("FabricPatch", ((chkFabricPatch.Checked) ? cmbfabricpatchGSM.Text + " + " + cmbfabricPatchLamGSM.Text : cmbfabricpatchGSM.Text),
                                ((chkfabricp.Checked) ? "Laminated" : "UnLaminated"), comboLoopColor.Text, FabricPatchSize.ToString(),
                              FabricPatchCutLength.ToString(), FabricPatcTotalMtr.ToString(), FabricPatchWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += FabricPatchWt;
                        }
                        #endregion
                        if (checkLoopCover.Checked)
                        {
                            string gsm = "";
                            string lami = "";
                            if (checkLoopCoverLami.Checked)
                                gsm = comboLoopCoverGSM.Text + " + " + comboLoopCoverLamiGSM.Text;
                            else
                                gsm = comboLoopCoverGSM.Text;

                            if (checkLoopCoverLami.Checked)
                                lami = "Laminated";
                            else
                                lami = "UnLaminated";

                            addDataRow("Loop Cover", gsm, lami, comboLoopCoverColor.Text, comboLoopCoverSize.Text.ToString(),
                            comboLoopCoverCutSize.Text.ToString(), LoopCOverTotalMtr.ToString(), LoopCoverWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += LoopCoverWt;
                        }

                        if (checkStevedorecover.Checked)
                        {
                            string gsm = "";
                            string lami = "";
                            if (checkStevedoreL.Checked)
                                gsm = comboSteveCoverGSM.Text + " + " + comboSteveCoverL.Text;
                            else
                                gsm = comboSteveCoverGSM.Text;

                            if (checkStevedoreL.Checked)
                                lami = "Laminated";
                            else
                                lami = "UnLaminated";

                            addDataRow("Stevedore Cover", gsm, lami, comboStevecoverColor.Text, StevecoverFabricSize.ToString(),
                            StevecoverLenght.ToString(), StevecoverTotalMtr.ToString(), SteveCoverWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += SteveCoverWt;
                        }
                        if (checkLoopProtector.Checked)
                        {
                            string gsm = "";
                            string lami = "";
                            if (checkLoopProcLami.Checked)
                                gsm = comboLoopProtectorGSM.Text + " + " + comboLoopProctectorLamiGSM.Text;
                            else
                                gsm = comboLoopProtectorGSM.Text;


                            if (checkLoopProcLami.Checked)
                                lami = "Laminated";
                            else
                                lami = "UnLaminated";

                            addDataRow("Loop Protector", gsm, lami, comboLoopProctectorColor.Text, LoopProtectorFabricSize.ToString(),
                            LoopProtectorCutLenght.ToString(), LoopProtectorTotalMtr.ToString(), LoopProtectorWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += LoopProtectorWt;

                        }

                        if (checkStevdore.Checked)
                        {
                            addDataRow("Stevedore", comboStGrm.Text, "", comboSteveDoreColor.Text, StevedoreFabricSize.ToString(),
                           StevedoreCutLenght.ToString(), StevedoreTotalMtr.ToString(), StevedoreWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += StevedoreWt;
                            count = 1;

                        }

                        if (checkBottomloop.Checked)
                        {
                            addDataRow("Bottom Loop", comboBottomLoopgrm.Text, "", comboBottomLoopColor.Text, BottomLoopSize.ToString(),
                           BottomLoopLenght.ToString(), BottomLoopTotalMtr.ToString(), BottomLoopWt.ToString(), " Cut Lenght" + textBottomLoopLenght.Text, SrNo, ref dataset2, "");

                            TotalKg += BottomLoopWt;
                            count = 1;

                        }


                        #endregion
                        if (checkBoxliner.Checked)
                        {
                            addDataRow("Liner", comboBoxlinermicron.Text, "", comboLinerColor.Text, LinerFabricSize.ToString(),
                             LinerCutLenght.ToString(), LinerTotalMtr.ToString(), LinerWt.ToString(), textLinerRemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += LinerWt;
                        }
                        if (checkBoxliner.Checked && checkBoxlinerBuffle.Checked) // Added By Rikin on 20-Apr-2015disusssed with dilen ji
                        {
                            addDataRow("Liner Baffle", textBuffleLinerMicron.Text, "", comboLinerColor.Text, LinerFabricSizeBuffle.ToString(),
                             LinerCutLenghtBuffle.ToString(), LinerBuffleTotalMtr.ToString(), LinerBuffleWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += LinerBuffleWt;
                        }
                        if (checkBoxdocpouch.Checked)
                        {
                            addDataRow("DocPouch", comboDocMicron.Text, "", comboDocColor.Text, DocFabricSize.ToString(),
                             DocCutLenght.ToString(), DocTotalMtr.ToString(), DocWt.ToString(), textDocRemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += DocWt;
                        }

                        if (checkdocpouch1.Checked)
                        {
                            addDataRow("DocPouch1", comboDoc1Micron.Text, "", comboDoc1Color.Text, Doc1FabricSize.ToString(),
                             Doc1CutLenght.ToString(), Doc1TotalMtr.ToString(), Doc1Wt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += Doc1Wt;
                        }

                        if (checkdocpouch2.Checked)
                        {
                            addDataRow("DocPouch2", comboDoc2Micron.Text, "", comboDoc2Color.Text, Doc2FabricSize.ToString(),
                             Doc2CutLenght.ToString(), Doc2TotalMtr.ToString(), Doc2Wt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += Doc2Wt;
                        }
                        if (checkBoxlabel.Checked)
                        {
                            addDataRow("Label", comboLabelMicron.Text, "", comboLabelColor.Text, LabelFabricSize.ToString(),
                            LabelCutLenght.ToString(), LabelTotalMtr.ToString(), LabelWt.ToString(), textLabelRemarks.Text, SrNo, ref dataset2, "");

                            TotalKg += LabelWt;
                        }

                        if (checkBoxTunnel.Checked)
                        {
                            string head = "";
                            if (comboTunnelDesign.SelectedIndex == 0 || comboTunnelDesign.SelectedIndex == 1)
                                head = "Reinforce fabric";
                            else
                                head = "Tunnel";

                            string micron = "";

                            if (checkTunnelLam.Checked)
                                micron = comboTunnelGSM.Text + " + " + comboTunnelLamiGSM.Text;
                            else
                                micron = comboTunnelGSM.Text;

                            addDataRow(head, micron, (checkTunnelLam.Checked ? "Laminated" : "UnLaminated"), "", TunnelFabricSize.ToString(),
                           TunnelCutLenght.ToString(), TunnelTotalMtr.ToString(), TunnelWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += TunnelWt;

                        }

                        if (_BodyIndex1 == 2) //Buffle
                        {
                            if (comboBuType.SelectedIndex == 1) //10.08.2021 changes in Buffle to NetBuffle
                            {
                                addDataRow(comboBuType.Text, "50", "", "",
                                 BuffleFabricSize.ToString(), BuffleCutLenght.ToString(), BuffleTotalMtr.ToString(), BuffleWt.ToString(), textBuffleRemarks.Text, SrNo, ref dataset2, "");
                            }
                            else
                            {

                                addDataRow("Buffle", textBuffleGSM.Text + "+" + textSingleCoatedGSM.Text + "+" + textDoubleCoatedGSM.Text, "Laminated", "",
                                    BuffleFabricSize.ToString(), BuffleCutLenght.ToString(), BuffleTotalMtr.ToString(), BuffleWt.ToString(), textBuffleRemarks.Text, SrNo, ref dataset2, "");
                            }
                            TotalKg += BuffleWt;
                        }

                        if (checkAncerieLoop.Checked)
                        {
                            //AncerieFabricSize.ToString()
                            addDataRow("Ancillay Loop", comboAncerieGrm.Text, comboAncillaryLooptype.Text, comboAncerieColor.Text,
                              comboAncerieWidth.Text, AncerieCutLenght.ToString(), AncerieTotalMtr.ToString(), AncerieWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += AncerieWt;
                        }


                        //Filler cord is Added to BOM2 by Rikin on 09-mar-2015
                        if (checkFillerCord.Checked)
                        {
                            addDataRow("Filler Cord", FillerCordGSM.ToString(), "", "Milky White",
                             "", "", FillerCordMtr.ToString(), FillerCordWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += FillerCordWt;
                        }

                        if (checkFelt.Checked)
                        {

                            addDataRow("Felt", "170", "", comboBodyColor.Text,
                              "", "", FeltMtr.ToString(), FeltWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += FeltWt;
                            if (checkFeltUnderTheLoop.Checked)
                            {
                                addDataRow("Felt-Under Loop", "170", "", comboBodyColor.Text,
                             FeltUnderTheLoopFabricSize.ToString(), FeltUnderTheLoopCutLenght.ToString(), FeltUnderTheLoopMtr.ToString(), FeltUnderTheLoopWt.ToString(), " ", SrNo, ref dataset2, "");
                                TotalKg += FeltUnderTheLoopWt;
                            }

                        }
                        if (checkFeltMfwebbing.Checked)
                        {
                            addDataRow("MFWeb", "15", "", comboBodyColor.Text,
                              "30", "", MFWebMtr.ToString(), MFWebWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += MFWebWt;
                        }

                        if (comboLoopConst.SelectedIndex == 3 && (_BodyIndex1 == 0 || _BodyIndex1 == 1
                      || _BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4))
                        {
                            addDataRow("Full Loop", "", "", "", FullLoopFabricSize.ToString(),
                             FullLoopCutLenght.ToString(), FullLoopTotalMtr.ToString(), FullLoopWt.ToString(), "", SrNo, ref dataset2, "");

                            TotalKg += FullLoopWt;
                        }


                        if (checkInnerBox.Checked)
                        {

                            string gsm = "";
                            string lami = "";
                            if (checkInnerBoxLami.Checked)
                                gsm = comboInnerBoxGSM.Text + " + " + comboInnerBoxLamiGSM.Text;
                            else
                                gsm = comboInnerBoxGSM.Text;

                            if (checkInnerBoxLami.Checked)
                                lami = "Laminated";
                            else
                                lami = "UnLaminated";

                            addDataRow("Inner Box", gsm, lami, comboInnerBoxColor.Text, InnerBoxFabricSize.ToString(),
                            InnerBoxCutLenght.ToString(), InnerBoxTotalMtr.ToString(), InnerBoxWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += InnerBoxWt;

                        }


                        if (checkInnerSkin.Checked)
                        {
                            string gsm = "";
                            string lami = "";
                            if (checkInnerSkinLami.Checked)
                                gsm = comboInnerSkinGSM.Text + " + " + comboInnerSkinLamiGSM.Text;
                            else
                                gsm = comboInnerSkinGSM.Text;

                            if (checkInnerSkinLami.Checked)
                                lami = "Laminated";
                            else
                                lami = "UnLaminated";


                            addDataRow("Inner Skin", gsm, lami, comboInnerSkinColor.Text, InnerSkinFabricSize.ToString(),
                        InnerSkinCutLenght.ToString(), InnerSkinTotalMtr.ToString(), InnerSkinWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += InnerSkinWt;

                        }

                        if (checkInnerTop.Checked)
                        {
                            string gsm = "";
                            string lami = "";

                            if (checkInnerTopLami.Checked)
                                gsm = comboInnerTopGSM.Text + " + " + comboInnerTopLamiGSM.Text;
                            else
                                gsm = comboInnerTopGSM.Text;

                            if (checkInnerTopLami.Checked)
                                lami = "Laminated";
                            else
                                lami = "UnLaminated";

                            addDataRow("Inner Top", gsm, lami, comboInnerTopColor.Text, InnerTopFabricSize.ToString(),
                        InnerTopCutLenght.ToString(), InnerTopTotalMtr.ToString(), InnerTopWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += InnerTopWt;

                        }

                        if (checkInnerBottom.Checked)
                        {

                            string gsm = "";
                            string lami = "";

                            if (checkInnerBottomLami.Checked)
                                gsm = comboInnerBottomGSM.Text + " + " + comboInnerBottomLamiGSM.Text;
                            else
                                gsm = comboInnerBottomGSM.Text;

                            if (checkInnerBottomLami.Checked)
                                lami = "Laminated";
                            else
                                lami = "UnLaminated";

                            addDataRow("Inner Bottom", gsm, lami, comboInnerBottomColor.Text, InnerBottomFabricSize.ToString(),
                      InnerBottomCutLenght.ToString(), InnerBottomTotalMtr.ToString(), InnerBottomWt.ToString(), "", SrNo, ref dataset2, "");
                            TotalKg += InnerBottomWt;

                        }

                        for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                        {
                            if (dataGridView1.Rows[i].Cells[0].Value != "")
                            {
                                if (dataGridView1.Rows[i].Cells[1].Value == null)
                                    dataGridView1.Rows[i].Cells[1].Value = "0";
                                if (dataGridView1.Rows[i].Cells[2].Value == null)
                                    dataGridView1.Rows[i].Cells[2].Value = "0";
                                if (dataGridView1.Rows[i].Cells[3].Value == null)
                                    dataGridView1.Rows[i].Cells[3].Value = "0";
                                if (dataGridView1.Rows[i].Cells[4].Value == null)
                                    dataGridView1.Rows[i].Cells[4].Value = "0";
                                if (dataGridView1.Rows[i].Cells[5].Value == null)
                                    dataGridView1.Rows[i].Cells[5].Value = "0";
                                if (dataGridView1.Rows[i].Cells[6].Value == null)
                                    dataGridView1.Rows[i].Cells[6].Value = "0";
                                if (dataGridView1.Rows[i].Cells[7].Value == null)
                                    dataGridView1.Rows[i].Cells[7].Value = "0";
                                if (dataGridView1.Rows[i].Cells[8].Value == null)
                                    dataGridView1.Rows[i].Cells[8].Value = ".";
                                if (dataGridView1.Rows[i].Cells[9].Value == null)
                                    dataGridView1.Rows[i].Cells[9].Value = "Other";

                               // MessageBox.Show(dataGridView1.Rows[i].Cells[9].FormattedValue.ToString());

                                addDataRow(dataGridView1.Rows[i].Cells[0].Value.ToString() + " ~", dataGridView1.Rows[i].Cells[1].Value.ToString()
                                    , dataGridView1.Rows[i].Cells[2].Value.ToString(), dataGridView1.Rows[i].Cells[3].Value.ToString()
                                    , dataGridView1.Rows[i].Cells[4].Value.ToString(),
                          dataGridView1.Rows[i].Cells[5].Value.ToString(), dataGridView1.Rows[i].Cells[6].Value.ToString(), dataGridView1.Rows[i].Cells[7].Value.ToString()
                          , dataGridView1.Rows[i].Cells[8].Value.ToString(), SrNo, ref dataset2, dataGridView1.Rows[i].Cells[9].FormattedValue.ToString());
                                TotalKg += Convert.ToDouble(dataGridView1.Rows[i].Cells[7].Value);

                            }
                        }


                        addDataRow("Thread", "", comboThreadType.Text, comboThreadColor.Text, "", "0".ToString(), "0".ToString(), Math.Round((ThreadWt / 100000), 4).ToString(), "", SrNo, ref dataset2, "");
                        //drs1[0] = "Thread";
                        ////drs1[1] = "";
                        ////drs1[2] = "";
                        ////drs1[3] = "";
                        ////drs1[4] = "";
                        ////drs1[5] = "";
                        ////drs1[6] = "";
                        //drs1[7] = Math.Round((ThreadWt / 100000), 4);
                        //drs1[8] = textFilePONo.Text;
                        //drs1[9] = SrNo;

                        //dataset2.Tables[0].Rows.Add(drs1);
                        TotalKg += Math.Round((ThreadWt / 100000), 4);

                        //if (count == 0)
                        //{
                        //    DataRow drs = dataset2.Tables[0].NewRow();
                        //    drs[8] = textFilePONo.Text;
                        //    drs[9] = SrNo;

                        //    dataset2.Tables[0].Rows.Add(drs);
                        //    if (SrNo != "temp")//If btn save is pressed then data will saved
                        //    {
                        //        cmd = new SqlCommandBuilder(myadpter1);
                        //        myadpter1.Update(dataset2.Tables[0]);
                        //    }
                        //}

                        //else
                        //{
                        if (SrNo != "temp")//If btn save is pressed then data will saved
                        {
                            cmd = new SqlCommandBuilder(myadpter1);
                            x = myadpter1.Update(dataset2.Tables[0]);
                        }
                        //}

                        #endregion
                        ///////////////////////////
                        #region BOM1 table Entry
                        SqlDataAdapter myadapter = Database.GetAdapterCommand("select SysDate,Customer,PrintType,FilePONo,BagType,SizeL,SizeW,SizeH,SizeType,SWL,Doc,ShortLen,LoopL,LoopW,LoopDim,S,L, "
                            + "FSType,LinerL,LinerW,LinerDim,Liner,LinerType,Qty,QtyUnit,DSL,DSW,DSDim,FabColor,DSType,FSL,FSW,  ThreadTotalKg, FeltWt,TotalKg,Instruction,BodyRemarks1,SrNo , "
                        + " SlitHt,FillHt,TotalHt,SideGSM,SideLami,BodyLami,BodyGSM,IsDropLoop,DropLoop,loopRemarks,DuffleHt,LinerCutSize,LinerFabric,pono,ponos,bodyno,isrf,printingremarks,conicaltop,refno,username,docl,docw,loopslitremarks,DSL1,DSW1,DSL2,DSW2,DSType1,DSType2,looptype,DocNumber,Bottomno,sideno,docunit,doc1,doc2 "
                        + " ,docl1,docw1,doc1unit,docl2,docw2,doc2unit,buffletype,loopconst,(select	top 1 v.MarketingInvNo from Despatch.dbo.MarketingInvoice v with(nolock) where v.BuyerOrderNo=FilePONo ) as MarketingInvNo,BottomSkritH,ApprovalField,looplongleg,Knottype,RPfabric,slitlenght from BOM1 WITH(nolock)  where 1=2");
                        DataSet dataset1 = new DataSet();
                        myadapter.Fill(dataset1);
                        DataRow dr = dataset1.Tables[0].NewRow();

                        if (checkRPFabric.Checked)
                            dr["RPfabric"] = "30% RP Fabric";
                        else
                            dr["RPfabric"] = " ";

                        dr["looplongleg"] = Utility.SafeConvertToDouble(textLongLeg.Text);
                        dr["slitlenght"] = Utility.SafeConvertToDouble(textSlitLength.Text);
                        dr["SysDate"] = EnqdateTime.Value.ToString("yyyy-MM-dd");
                        dr["Customer"] = comboPartyName.Text;
                        dr["PrintType"] = comboPrintType.Text;
                        dr["looptype"] = comboLoopType.Text;
                        dr["DocUnit"] = comboDocUnit.Text;
                        dr["FilePONo"] = textFilePONo.Text;
                        if (textDocW.Text == "")
                            textDocW.Text = "0";
                        if (textDocL.Text == "")
                            textDocL.Text = "0";

                        dr["docl"] = textDocL.Text;
                        dr["docw"] = textDocW.Text;

                        if (SrNo != "temp")
                            dr["pono"] = textpono.Text;
                        dr["ponos"] = textpono.Text;
                        if (checkBoxTunnel.Checked)
                            if (comboBagType.Text.Length > 0)
                                dr["BagType"] = comboBody1.Text + "/Tunnel Bag " + "/" + comboBagType.Text;
                            else
                                dr["BagType"] = comboBody1.Text + "/Tunnel Bag ";
                        else
                            if (comboBagType.Text.Length > 0)
                                dr["BagType"] = comboBody1.Text + "/" + comboBody2.Text + "/" + comboBody3.Text + "/" + comboBagType.Text;
                            else
                                dr["BagType"] = comboBody1.Text + "/" + comboBody2.Text + "/" + comboBody3.Text;
                        dr["SizeL"] = textBodyL.Text;
                        dr["SizeW"] = textBodyW.Text;
                        dr["SizeH"] = textBodyH.Text;
                        dr["SizeType"] = comboType.Text;

                        dr["SWL"] = textSWL.Text;
                        if (checkBoxdocpouch.Checked)
                            dr["Doc"] = comboDocType.Text + "/" + comboDocType1.Text + "/" + comboDocType2.Text + "/" + textDocNo.Text;
                        else
                            dr["Doc"] = "N/A";


                        if (checkdocpouch1.Checked)
                        {
                            dr["Doc1"] = combodoctype3.Text + "/" + combodoctype4.Text + "/" + combodoctype5.Text + "/" + textdoc1No.Text;
                            dr["docl1"] = textDoc1L.Text;
                            dr["docw1"] = textDoc1W.Text;
                            dr["doc1unit"] = comboDoc1Unit.Text;
                        }
                        else
                            dr["Doc1"] = "N/A";

                        if (checkdocpouch2.Checked)
                        {
                            dr["Doc2"] = combodoctype6.Text + "/" + combodoctype7.Text + "/" + combodoctype8.Text + "/" + textdoc2No.Text;
                            dr["docl2"] = textDoc2L.Text;
                            dr["docw2"] = textDoc2W.Text;
                            dr["doc2unit"] = comboDoc2Unit.Text;
                        }
                        else
                            dr["Doc2"] = "N/A";



                        dr["LoopDim"] = comboBodyUnit.Text;
                        dr["S"] = comboSF.Text;
                        dr["L"] = textSWL.Text;
                        if (checkBoxTop.Checked && comboTopType.SelectedIndex != 0)
                        {
                            if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 6 ||
                                comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8 || comboTopType.SelectedIndex == 9) //17.6.2021
                                dr["FSType"] = comboTopType.Text;
                            else
                            {
                                //  dr[16] = comboTopType.Text;  need ta add in other table
                                dr["FSType"] = comboSpoutType.Text;
                            }
                        }
                        else
                            dr[17] = "Open";


                        if (checkBoxliner.Checked)
                        {
                            dr["LinerL"] = comboBoxlinerheight.Text;
                            dr["LinerW"] = comboBoxlinerwidth.Text;
                            dr["LinerDim"] = comboBoxlinermicron.Text;

                            dr["Liner"] = comboBoxlinertype.Text;
                            if (checkBoxlinerBuffle.Checked)
                                dr["LinerType"] = comboBoxlinertype1.Text + "| Baffle Liner";
                            else
                                dr["LinerType"] = comboBoxlinertype1.Text;
                        }
                        else
                            dr["LinerType"] = "N/A";

                        dr["Qty"] = textQty.Text;
                        //QtyUnit at 24th location 
                        if (checkbottom.Checked)
                        {
                            dr["DSL"] = comboBoxbottomdia.Text;
                            dr["DSW"] = comboBoxbottomheight.Text;
                            dr["DSType"] = comboBoxbottomtype.Text + "/" + comboBoxbottomsubtype.Text;
                        }
                        else
                            dr["DSType"] = "Flat";

                        if (checkBottom1.Checked)
                        {
                            dr["DSL1"] = comboBoxbottomdia1.Text;
                            dr["DSW1"] = comboBoxbottomheight1.Text;
                            dr["DSType1"] = comboBoxbottomtype1.Text + "/" + comboBoxbottomsubtype1.Text;
                        }
                        else
                            dr["DSType1"] = "Flat";


                        if (checkBottom2.Checked)
                        {
                            dr["DSL2"] = comboBoxbottomdia2.Text;
                            dr["DSW2"] = comboBoxbottomheight2.Text;
                            dr["DSType2"] = comboBoxbottomtype2.Text + "/" + comboBoxbottomsubtype2.Text;
                        }
                        else
                            dr["DSType2"] = "Flat";


                        dr["FabColor"] = comboBodyColor.Text;

                        //DSDim "27th location

                        if (checkBoxTop.Checked)
                        {
                            if (comboSpoutDia.Text == "")
                                comboSpoutDia.Text = "0";
                            if (comboSpoutHeight.Text == "")
                                comboSpoutHeight.Text = "0";
                            dr["FSL"] = comboSpoutDia.Text;
                            dr["FSW"] = comboSpoutHeight.Text;
                        }
                        //Code Moved to new table

                        dr["FeltWt"] = FeltWt;
                        dr["TotalKg"] = TotalKg;
                        dr["Instruction"] = textInstruction.Text;
                        dr["BodyRemarks1"] = textBodyRemarks.Text;
                        dr["SrNo"] = SrNo;
                        if (_BodyIndex1 == 5 || _BodyIndex1 == 6 || _BodyIndex1 == 7 || _BodyIndex1 == 8)
                        {
                            dr["SlitHt"] = SlitHt;
                            dr["FillHt"] = textFillHt.Text;
                            dr["TotalHt"] = TotalHt;
                        }
                        dr["BodyGSM"] = comboBodyGSM.Text;
                        dr["BodyLami"] = comboBodyLamiGSM.Text;
                        dr["SideGSM"] = comboSideGSM.Text;
                        dr["SideLami"] = comboSideLamiGSM.Text;
                        dr["IsDropLoop"] = (checkBoxDropLoop.Checked ? "yes" : "no");
                        dr["DropLoop"] = textLoopDropLenght.Text;
                        dr["LinerFabric"] = LinerFabricSize.ToString();
                        dr["LinerCutSize"] = LinerCutLenght.ToString();
                        dr["bodyno"] = textBodyNo.Text;
                        dr["isrf"] = (checkBoxRF.Checked ? "yes" : "no");
                        dr["printingremarks"] = textprintingremarks.Text;
                        dr["refno"] = textRefNo.Text;
                        dr["conicaltop"] = textConicaltop.Text;
                        dr["username"] = FrmMainForm.UserName;

                        if (_BodyIndex1 == 5 || _BodyIndex1 == 6)
                        {
                            string t = "SLIT HEIGHT ";
                            if (textSlitHt.Text == "")
                                textSlitHt.Text = "0";
                            if (Convert.ToInt32(textSWL.Text) <= 1499)
                                t += Convert.ToString(Convert.ToInt32(textSlitHt.Text) + 15) + " CM";
                            else
                                t += Convert.ToString(Convert.ToInt32(textSlitHt.Text) + 20) + " CM";

                            dr["loopslitremarks"] = t;

                        }
                        dr["loopslitremarks"] = "";
                        dr["Bottomno"] = textBottomNo.Text;
                        dr["sideno"] = textSideNo.Text;

                        if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 6 ||
                            comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8 || comboTopType.SelectedIndex == 9) //Duffle 17.6.2021
                            dr["DuffleHt"] = comboBoxduffleskirtheight.Text;

                        if (_BodyIndex1 == 2)//Buffle
                            dr["BuffleType"] = comboBuffleType.Text;

                        dr["loopRemarks"] = textLoopNo.Text;
                        dr["DocNumber"] = textDocNo.Text;
                        dr["loopconst"] = comboLoopConst.Text;

                        if (checkBoxLoop.Checked)
                        {
                            dr["ShortLen"] = textShortLeg.Text;
                            dr["LoopL"] = comboLoopL.Text;
                            dr["LoopW"] = comboLoopW.Text;
                        }
                        if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5 || comboTopType.SelectedIndex == 6 ||
                            comboTopType.SelectedIndex == 7 || comboTopType.SelectedIndex == 8 || comboTopType.SelectedIndex == 9) //Duffle 17.06.2021
                            dr["DuffleHt"] = comboBoxduffleskirtheight.Text;

                        if (_BodyIndex1 == 2)//Buffle
                            dr["BuffleType"] = comboBuffleType.Text;

                        dr["loopRemarks"] = textLoopNo.Text;
                        dr["DocNumber"] = textDocNo.Text;
                        if (textSkirtHeight.Text.Length > 0)
                            dr["BottomSkritH"] = textSkirtHeight.Text; //20.09.2021

                        string Approvalfield = "";
                        for (int i = 0; i < dgapprovallist.Rows.Count - 1; i++)
                        {
                            if (dgapprovallist.Rows[i].Cells[0].FormattedValue.ToString() == "True")
                            {
                                Approvalfield += dgapprovallist.Rows[i].Cells[1].Value.ToString() + Environment.NewLine;
                            }
                        }
                        dr["Approvalfield"] = Approvalfield;
                        dr["Knottype"] = comboBottomKnotType.Text;
                        #region commented Code
                        //By Rikin on 19-Mar-2015 Need to Add after checking

                        //if (checkbottom.Checked)
                        //    dr[127] = textBottomRemarks.Text;
                        //if (checkSpoutTie.Checked)
                        //    dr[128] = textTopSpoutTieRemarks.Text;
                        //if (checkBottomSpoutTie.Checked)
                        //    dr[129] = textBottomSpoutTieRemarks.Text;

                        //if (checkBoxDropLoop.Checked)
                        //{
                        //    dr[130] = "yes";
                        //    dr[131] = textLoopDropLenght.Text;
                        //}

                        //if (textRMPP.Text != "")
                        //    dr[132] = textRMPP.Text;
                        //else
                        //{
                        //    dr[132] = "0";
                        //    textRMPP.Text = "0";
                        //}
                        //if (textStdConvPP.Text != "")
                        //    dr[133] = textStdConvPP.Text;
                        //else
                        //{
                        //    dr[133] = "0";
                        //    textStdConvPP.Text = "0";
                        //}


                        //if (textRMPE.Text != "")
                        //    dr[134] = textRMPE.Text;
                        //else
                        //{
                        //    dr[134] = "0";
                        //    textRMPE.Text = "0";
                        //}
                        //if (textStdConvPE.Text != "")
                        //    dr[135] = textStdConvPE.Text;
                        //else
                        //{
                        //    dr[135] = "0";
                        //    textStdConvPE.Text = "0";
                        //}

                        //if (checkBoxdocpouch.Checked)
                        //    dr[136] = textDoc.Text;
                        //dr[137] = textPallets.Text;
                        //if (textPrintingRate.Text == "")
                        //    textPrintingRate.Text = "0";
                        //dr[138] = textPrintingRate.Text;
                        //dr[139] = textBLock.Text;
                        //dr[140] = textHoseSlider.Text;
                        //dr[141] = textVelcro.Text;
                        //dr[142] = textDustProof.Text;
                        //dr[143] = textFelt.Text;

                        //if (textFrieght.Text == "")
                        //    textFrieght.Text = "0";

                        //dr[144] = textFrieght.Text;

                        ////Group A Rate Wt Calcualtion

                        //#region //Group A Rate Wt Calcualtion
                        //double TotalGroupAWt = 0;
                        //double TotalGroupARate = 0;
                        //if (checkBoxliner.Checked)
                        //    TotalGroupAWt = TotalGroupAWt + LinerWt;
                        //if (checkBoxdocpouch.Checked)
                        //    TotalGroupAWt = TotalGroupAWt + DocWt;
                        //if (checkFillerCord.Checked)
                        //    TotalGroupAWt = TotalGroupAWt + FillerCordWt;
                        //if (comboFillerCord.SelectedIndex == 3)
                        //    TotalGroupAWt = TotalGroupAWt + FeltWt;

                        //TotalGroupARate = Utility.SafeConvertToDouble(textRMPP.Text)
                        //                    + Utility.SafeConvertToDouble(textStdConvPP.Text);

                        //TotalGroupARate = TotalGroupARate / 1000;

                        //TotalGroupAWt = TotalKg - TotalGroupAWt;
                        //// TotalGroupARate = TotalGroupARate * TotalGroupAWt;
                        ////////////////////////////

                        //#endregion
                        ////Group B Rate Wt Calcualtion
                        //#region Group B Rate Wt Calcualtion
                        //double TotalGroupBWt = 0;
                        //double TotalGroupBRate = 0;
                        //if (checkBoxliner.Checked)
                        //{
                        //    TotalGroupBWt = TotalGroupBWt + LinerWt;

                        //    TotalGroupBRate = Utility.SafeConvertToDouble(textRMPE.Text)
                        //                        + Utility.SafeConvertToDouble(textStdConvPE.Text);

                        //    TotalGroupBRate = TotalGroupBRate / 1000;

                        //    if (combolinersubtype.SelectedIndex == 1) //Tabbed
                        //    {
                        //        if (comboBoxlineratpoint.Text == "4")
                        //            TotalGroupBRate = TotalGroupBRate + .17;
                        //        else if (comboBoxlineratpoint.Text == "8")
                        //            TotalGroupBRate = TotalGroupBRate + .34;
                        //    }

                        //    if (combolinersubtype.SelectedIndex == 2) //Glued
                        //    {
                        //        if (comboBoxlineratpoint.Text == "4")
                        //            TotalGroupBRate = TotalGroupBRate + .10;
                        //        else if (comboBoxlineratpoint.Text == "8")
                        //            TotalGroupBRate = TotalGroupBRate + .28;
                        //    }
                        //    // TotalGroupBRate = TotalGroupBRate * TotalGroupBWt;
                        //}
                        ////////////////////////////

                        //#endregion
                        ////Group C Rate Wt Calcualtion
                        //#region Group C Rate Wt Calcualtion
                        //double TotalGroupCWt = 0;
                        //double TotalGroupCRate = 0;
                        //if (checkFillerCord.Checked)
                        //{
                        //    TotalGroupCWt = TotalGroupCWt + FillerCordWt;
                        //    TotalGroupCRate = TotalGroupCRate + (Utility.SafeConvertToDouble(textDustProof.Text)
                        //         * FillerCordWt);
                        //}
                        //if (comboFillerCord.SelectedIndex == 3)
                        //{
                        //    TotalGroupCWt = TotalGroupCWt + FeltWt;
                        //    TotalGroupCRate = TotalGroupCRate + (Utility.SafeConvertToDouble(textFelt.Text)
                        //        * FeltWt);
                        //}
                        ////////////////////////////

                        //#endregion
                        ////Group D Rate Wt Calcualtion (Add Ons)
                        //#region  Group D Rate Wt Calcualtion (Add Ons)
                        //double TotalGroupDRate = 0;
                        //if (checkBoxdocpouch.Checked)
                        //    TotalGroupDRate = TotalGroupDRate + Utility.SafeConvertToDouble(textDoc.Text);
                        //TotalGroupDRate = TotalGroupDRate + Utility.SafeConvertToDouble(textPallets.Text);
                        //TotalGroupDRate = TotalGroupDRate + Utility.SafeConvertToDouble(textPrintingRate.Text);
                        //if (checkBoxblock.Checked)
                        //    TotalGroupDRate = TotalGroupDRate +
                        //         (Utility.SafeConvertToDouble(textBLock.Text) * Utility.SafeConvertToDouble(textBoxblocknos.Text));
                        //if (checkTopVelcro.Checked)
                        //    TotalGroupDRate = TotalGroupDRate +
                        //          (Utility.SafeConvertToDouble(textVelcro.Text) * Utility.SafeConvertToDouble(comboTopVelcro.Text));
                        //if (checkbottomvelcro.Checked)
                        //    TotalGroupDRate = TotalGroupDRate +
                        //          (Utility.SafeConvertToDouble(textVelcro.Text) * Utility.SafeConvertToDouble(combobottomvelcro.Text));

                        //if (checkTopHoseSlider.Checked)
                        //    TotalGroupDRate = TotalGroupDRate +
                        //          (Utility.SafeConvertToDouble(textHoseSlider.Text) * Utility.SafeConvertToDouble(comboTopHoseSlider.Text));
                        //if (checkBottomhoseslider.Checked)
                        //    TotalGroupDRate = TotalGroupDRate +
                        //          (Utility.SafeConvertToDouble(textHoseSlider.Text) * Utility.SafeConvertToDouble(comboBottomhoseslider.Text));

                        ////////////////////////////
                        //double TotalGroupERate = 0;
                        //if ((comboLoopType.SelectedIndex == 1 || comboLoopType.SelectedIndex == 2) && checkBoxLoop.Checked) //MultiFilament,SeatBelt
                        //{
                        //    if (textLoopStdConv.Text == "")
                        //        textLoopStdConv.Text = "0";
                        //    TotalGroupERate = LoopWt * Utility.SafeConvertToDouble(textLoopStdConv.Text);
                        //    TotalGroupERate = TotalGroupERate / 1000;
                        //}

                        //if (textFrieght.Text == "")
                        //    textFrieght.Text = "0";

                        //double TotalFrieght = (TotalKg - DocWt) * Utility.SafeConvertToDouble(textFrieght.Text);
                        //TotalFrieght = TotalFrieght / 1000;

                        //if (comboCurrency.SelectedIndex == 0)
                        //{
                        //    TotalGroupARate = TotalGroupARate * Utility.SafeConvertToDouble(textINR.Text);
                        //    TotalGroupBRate = TotalGroupBRate * Utility.SafeConvertToDouble(textINR.Text);
                        //    TotalGroupCRate = TotalGroupCRate * Utility.SafeConvertToDouble(textINR.Text);
                        //    TotalGroupDRate = TotalGroupDRate * Utility.SafeConvertToDouble(textINR.Text);
                        //    TotalGroupERate = TotalGroupERate * Utility.SafeConvertToDouble(textINR.Text);
                        //    TotalFrieght = TotalFrieght * Utility.SafeConvertToDouble(textINR.Text);
                        //}
                        //if (comboCurrency.SelectedIndex == 2)
                        //{
                        //    TotalGroupARate = TotalGroupARate * Utility.SafeConvertToDouble(textGBP.Text);
                        //    TotalGroupBRate = TotalGroupBRate * Utility.SafeConvertToDouble(textGBP.Text);
                        //    TotalGroupCRate = TotalGroupCRate * Utility.SafeConvertToDouble(textGBP.Text);
                        //    TotalGroupDRate = TotalGroupDRate * Utility.SafeConvertToDouble(textGBP.Text);
                        //    TotalGroupERate = TotalGroupERate * Utility.SafeConvertToDouble(textGBP.Text);
                        //    TotalFrieght = TotalFrieght * Utility.SafeConvertToDouble(textGBP.Text);

                        //}
                        //if (comboCurrency.SelectedIndex == 3)
                        //{
                        //    TotalGroupARate = TotalGroupARate * Utility.SafeConvertToDouble(textEURO.Text);
                        //    TotalGroupBRate = TotalGroupBRate * Utility.SafeConvertToDouble(textEURO.Text);
                        //    TotalGroupCRate = TotalGroupCRate * Utility.SafeConvertToDouble(textEURO.Text);
                        //    TotalGroupDRate = TotalGroupDRate * Utility.SafeConvertToDouble(textEURO.Text);
                        //    TotalGroupERate = TotalGroupERate * Utility.SafeConvertToDouble(textEURO.Text);
                        //    TotalFrieght = TotalFrieght * Utility.SafeConvertToDouble(textEURO.Text);
                        //}
                        //if (textDiscount.Text == "")
                        //    textDiscount.Text = "0";
                        //double TotalRate = (TotalGroupARate * TotalGroupAWt)
                        //                   + (TotalGroupBRate * TotalGroupBWt)
                        //                   + (TotalGroupCRate * TotalGroupCWt)
                        //                   + TotalGroupDRate + TotalGroupERate
                        //                    + TotalFrieght - Utility.SafeConvertToDouble(textDiscount.Text);

                        //dr[145] = TotalGroupAWt;
                        //dr[146] = TotalGroupARate;
                        //dr[147] = TotalGroupBWt;
                        //dr[148] = TotalGroupBRate;
                        //dr[149] = TotalGroupCWt;
                        //dr[150] = TotalGroupCRate;
                        //dr[151] = TotalGroupDRate;
                        //dr[152] = TotalFrieght;
                        //dr[153] = TotalRate;

                        //#endregion

                        //if (textFile1.Text.Trim().Length != 0)
                        //{
                        //    string fileName = Path.GetFileName(textFile1.Text.ToString());
                        //    byte[] content = ReadFileToByteArray(fileName);
                        //    dr[154] = 1;
                        //    dr[155] = fileName;
                        //    dr[156] = content;
                        //}



                        //dr[158] = comboCurrency.Text;
                        //dr[159] = textDiscount.Text;
                        //if (checkOrderConfirmed.Checked)
                        //    dr[160] = "Confirmed";

                        //dr[161] = TotalGroupERate;
                        //if (textLoopStdConv.Text == "")
                        //    dr[162] = "0";
                        //else
                        //    dr[162] = textLoopStdConv.Text;
                        //dr[163] = comboLoopType.Text;

                        //if (_BodyIndex1 == 2)//Buffle
                        //    dr[164] = comboBuffleType.Text;

                        //dr[165] = textTopRemarks.Text;

                        //dr[167] = textLinerRemarks.Text;
                        //dr[168] = textLabelRemarks.Text;
                        //dr[169] = textPerson.Text;

                        //if (comboBoxbottomtype.SelectedIndex == 8) // Bottom Duffle
                        //    dr[170] = textSkirtHeight.Text;
                        //if (checkBoxdocpouch.Checked)
                        //    dr[171] = textDocNo.Text;


                        #endregion

                        dataset1.Tables[0].Rows.Add(dr);
                        #endregion
                        if (SrNo != "temp")//If btn save is pressed then data will saved
                        {
                            if (comboConicalHeight.Text == "")
                                comboConicalHeight.Text = "0";
                            if (textStartSewnBaseHt.Text == "")
                                textStartSewnBaseHt.Text = "0";
                            if (textStNo.Text == "")
                                textStNo.Text = "0";
                            if (textFSNo.Text == "")
                                textFSNo.Text = "0";
                            if (textDSNo.Text == "")
                                textDSNo.Text = "0";
                            if (textDSNo1.Text == "")
                                textDSNo1.Text = "0";
                            if (textDSNo2.Text == "")
                                textDSNo2.Text = "0";

                            cmd = new SqlCommandBuilder(myadapter);
                            x = myadapter.Update(dataset1.Tables[0]);


                            #region BOM3 Table Entry
                            //Inserting Pending data which are not saving in any of above table. By Rikin on 28-Feb-2015
                            Database.GetExecuteNonQueryCommand("Insert into BOM3(SrNo,PONO ,RF,Thread,Hiracle,HiracleTop,HIracleBottom,ThreadBuffleSeam,ThreadNeedle,DropLoop,TillTheBottom,LoopLength,isfillercord,Fillercord,fillercordtop,fillercordbottom,fillercordtopspout,fillercordbottomspout,fillercordbody,fillercordbuffle,ThreadColor,threadtype,threaddenier,feltbody,lineratpoint,toptypes,bottomtypes,felttop,feltbottom,felttopspout,feltbottomspout,feltundertheloop,fsno,dsno,dsno1," +
                                " dsno2,conicalheight,StartSewnBaseHt,Stevdoreno,topno,topflapno,bottomflapno,bottomloopno,BuffleType, BuffleSideA, BuffleSideB,SubBuffleType,feltMFWeb,DoubleFoldBody,DoubleFoldTop,DoubleFoldBottom,DoubleFoldBottomSpout,DoubleFoldBottomSpout2,BoxSpoutConical,IsTopVelcro,TopVelcro,IsTopHoseSlider,TopHoseSlider,IsCableTie,CableTie," +
                                " bottomgsm1,Isbottomlam1,BottomSubTypeLamiGSM,Isbottomtieextra,Isbottomvelcro,bottomvelcro,IsBottomhoseslider,Bottomhoseslider,IsBoxbottomwiretie,bottomwiretie,Isbottomcabletie,bottomcabletie,BottomConicalHeight,BottomSubTypeRemarks,BottomSubTypeColor )" +
                                " values('"
                                + SrNo + "','" + textFilePONo.Text + "','" + (checkBoxRF.Checked ? "Yes" : "No") + "','" + (checkThread.Checked ? "Yes" : "No") + "','"
                                + (checkHiracle.Checked ? "Yes" : "No") + "','" + (checkHiracleTop.Checked ? "Yes" : "No") + "','" + (checkHiracleBottom.Checked ? "Yes" : "No")
                                + "','" + comboThreadBuffleSeam.Text + "','" + comboThreadNeedle.Text + "','" + (checkBoxDropLoop.Checked ? "Yes" : "No") + "','" + (checkLoopTillBottom.Checked ? "Yes" : "No")
                                + "','" + textLoopLenght.Text + "','" + (checkFillerCord.Checked ? "Yes" : "No") + "','" + comboFillerCordTop.Text + "','" + (checkFillerTop.Checked ? "Yes" : "No") + "','" + (checkFillerBottom.Checked ? "Yes" : "No")
                                + "','" + (checkFillerTopSpout.Checked ? "Yes" : "No") + "','" + (checkFillerBottomSpout.Checked ? "Yes" : "No")
                                + "','" + (CheckFillerBody.Checked ? "Yes" : "No") + "','" + comboBuffleSeam.Text + "','" + comboThreadColor.Text + "','" + comboThreadType.Text + "','" + textThreadDenier.Text + "','" + (checkFeltBody.Checked ? "Yes" : "No") + "','" + comboBoxlineratpoint.Text + "','" + comboTopType.Text + "','" + comboBoxbottomtype.Text + "','" + (checkFeltTop.Checked ? "Yes" : "No")
                                + "','" + (checkFeltBottom.Checked ? "Yes" : "No") + "','" + (checkFeltTopSpout.Checked ? "Yes" : "No") + "','" + (checkFeltBottomSpout.Checked ? "Yes" : "No") + "','" + (checkFeltUnderTheLoop.Checked ? "Yes" : "No") + "'," + textFSNo.Text + "," + textDSNo.Text + "," + textDSNo1.Text + "," + textDSNo2.Text + "," + comboConicalHeight.Text + "," + textStartSewnBaseHt.Text + "," + textStNo.Text + "," + textTopNo.Text + ","
                                + textBoxtopflapnosflap.Text + "," + txtBottomFlap.Text + "," + textBottomLoopNo.Text + ",'" + comboBuType.Text + "',"
                                + Utility.SafeConvertToDouble(txtBuffSideA.Text) + "," + Utility.SafeConvertToDouble(txtBuffSideB.Text) + ",'"
                                + cmbSubBufType.Text + "','" + (checkFeltMfwebbing.Checked ? "Yes" : "No") + "','"
                                + (checkBoxDoubleFoldBody.Checked ? "Yes" : "No") + "','"
                                + (checkBoxDoubleFoldTop.Checked ? "Yes" : "No") + "','"
                                + (checkBoxDoubleFoldBottom.Checked ? "Yes" : "No") + "','"
                                + (checkBoxDoubleFoldBottomSpout.Checked ? "Yes" : "No") + "','"
                                + (checkBoxDoubleFoldBottomSpout2.Checked ? "Yes" : "No") + "','"
                                + (checkBoxSpoutConical.Checked ? "Yes" : "No") + "','"
                                + (checkTopVelcro.Checked ? "Yes" : "No") + "','"
                                + comboTopVelcro.Text + "','" + (checkTopHoseSlider.Checked ? "Yes" : "No") + "','"
                                + comboTopHoseSlider.Text + "','" + (checkBoxCableTie.Checked ? "Yes" : "No") + "','"
                                + textBoxCableTie.Text + "','" + comboBoxbottomgsm1.Text + "','"
                                + (checkBoxbottomlam1.Checked ? "Yes" : "No") + "','"
                                + comboBoxBottomSubTypeLamiGSM.Text + "','"
                                + (checkBoxbottomtieextra.Checked ? "Yes" : "No") + "','"
                                + (checkbottomvelcro.Checked ? "Yes" : "No") + "','" + combobottomvelcro.Text + "','"
                                + (checkBottomhoseslider.Checked ? "Yes" : "No") + "','" + comboBottomhoseslider.Text + "','"
                                + (checkBoxbottomwiretie.Checked ? "Yes" : "No") + "','" + textBoxbottomwiretie.Text + "','"
                                + (checkBoxbottomcabletie.Checked ? "Yes" : "No") + "','" + textBoxbottomcabletie.Text + "','"
                                + comboConicalHeight.Text + "','" + textBottomRemarks.Text + "','" + comboBottomSpoutColor.Text + "') ");
                            //BoxSpoutConical,IsTopVelcro,TopVelcro,IsTopHoseSlider,TopHoseSlider,IsCableTie,CableTie

                            string strupdate = "UPDATE BOM3 set "
                                + " IsBS1bottomtieextra1='" + (checkBoxbottomtieextra1.Checked ? "Yes" : "No") + "',"
                                + " IsBS1bottomvelcro1='" + (checkbottomvelcro1.Checked ? "Yes" : "No") + "',"
                                + " BS1bottomvelcro1 ='" + combobottomvelcro1.Text + "',"
                                + " IsBS1Bottomhoseslider1='" + (checkBottomhoseslider1.Checked ? "Yes" : "No") + "',"
                                + " BS1Bottomhoseslider1='" + comboBottomhoseslider1.Text + "',"
                                + " IsBS1bottomwiretie1='" + (checkBoxbottomwiretie1.Checked ? "Yes" : "No") + "',"
                                + " BS1bottomwiretie1='" + textBoxbottomwiretie1.Text + "',"
                                + " IsBS1bottomcabletie1='" + (checkBoxbottomcabletie1.Checked ? "Yes" : "No") + "',"
                                + " BS1bottomcabletie1='" + textBoxbottomcabletie1.Text + "',"
                                + " BS1SkirtHeight1='" + textSkirtHeight1.Text + "',"
                                + " BS1Bottomrem1='" + textBottomrem1.Text + "',"
                                + " BS1bottomgsm2='" + comboBoxbottomgsm2.Text + "',"
                                + " IsBS1bottomlam2='" + (checkBoxbottomlam2.Checked ? "Yes" : "No") + "',"
                                + " BS1BottomLamiGSM1='" + comboBottomLamiGSM1.Text + "',"
                                + " BS1BottomNo1='" + textBottomNo1.Text + "',"
                                + " BS1BottomColor1='" + comboBottomColor1.Text + "',"
                                + " BS1SpoutRope1='" + comboBottomSpoutRope1.Text + "',"
                                + " BS1SpoutRopeSize1='" + comboBottomSpoutRopeSize1.Text + "',"
                                + " BS1SpoutRopeNo1='" + textBottomSpoutRopeNo1.Text + "',"
                                + " BS1SpoutRopeColor1='" + comboBottomSpoutRopeColor1.Text + "',"
                                + " BS1spoutroperemarks1='" + textBottomspoutroperemarks1.Text + "',"
                                + " ISBottomSpoutRope1='" + (checkBottomSpoutRope1.Checked ? "Yes" : "No") + "',"
                                + " BS1TieGrm1='" + comboBottomSpoutTieGrm1.Text + "',"
                                + " BS1TieSize1='" + comboBottomSpoutTieSize1.Text + "',"
                                + " BS1TieCutSize1='" + comboBottomSpoutTieCutSize1.Text + "',"
                                + " BS1SpoutTieNo1='" + textBottomSpoutTieNo1.Text + "'  "
                                + " WHERE SRNO='" + SrNo + "' AND PONO='" + textFilePONo.Text + "'";
                            Database.GetExecuteNonQueryCommand(strupdate);

                            strupdate = "UPDATE BOM3 SET "
                                + " BS2bottomgsm4='" + comboBoxbottomgsm4.Text + "',"
                                + " IsBS2bottomlam4='" + (checkBottomSpoutRope1.Checked ? "Yes" : "No") + "',"
                                + " BS2LamiGSM2='" + comboBottomLamiGSM2.Text + "',"
                                + " BS2No2='" + textBottomNo2.Text + "',"
                                + " BS2Color2='" + comboBottomColor2.Text + "',"
                                + " BS2rem2='" + textBottomrem2.Text + "',"
                                + " IsBS2tieextra2='" + (checkBoxbottomtieextra2.Checked ? "Yes" : "No") + "',"
                                + " IsBS2velcro2='" + (checkbottomvelcro2.Checked ? "Yes" : "No") + "',"
                                + " BS2velcro2='" + combobottomvelcro2.Text + "',"
                                + " IsBS2hoseslider2='" + (checkBottomhoseslider2.Checked ? "Yes" : "No") + "',"
                                + " BS2hoseslider2='" + comboBottomhoseslider2.Text + "',"
                                + " IsBS2wiretie2='" + (checkBoxbottomwiretie2.Checked ? "Yes" : "No") + "',"
                                + " BS2wiretie2='" + textBoxbottomwiretie2.Text + "',"
                                + " IsBS2cabletie2='" + (checkBoxbottomcabletie2.Checked ? "Yes" : "No") + "',"
                                + " BS2cabletie2='" + textBoxbottomcabletie2.Text + "',"
                                + " BS2SkirtHeight2='" + textSkirtHeight2.Text + "',"
                                + " BS2SpoutRope2='" + comboBottomSpoutRope2.Text + "',"
                                + " BS2SpoutRopeSize2='" + comboBottomSpoutRopeSize2.Text + "',"
                                + " BS2SpoutRopeNo2='" + textBottomSpoutRopeNo2.Text + "',"
                                + " BS2TieSize2='" + comboBottomSpoutTieSize2.Text + "',"
                                + " BS2TieCutSize2='" + comboBottomSpoutTieCutSize2.Text + "',"
                                + " BS2TieNo2='" + textBottomSpoutTieNo2.Text + "',"
                                + " ISlabel='" + (checkBoxlabel.Checked ? "Yes" : "No") + "',"
                                + " Isblock='" + (checkBoxblock.Checked ? "Yes" : "No") + "',"
                                + " blocknos='" + textBoxblocknos.Text + "'"
                                + " WHERE SRNO='" + SrNo + "' AND PONO='" + textFilePONo.Text + "'";
                            Database.GetExecuteNonQueryCommand(strupdate);

                            strupdate = "UPDATE BOM3 SET "
                                    + "InnerSkinExtraCutLenght='" + textInnerSkinExtraCutLenght.Text + "',"
                                    + "InnerTopSize='" + comboInnerTopSize.Text + "',"
                                    + "InnerBottomSize='" + comboInnerBottomSize.Text + "',"
                                    + "InnerSkinSize='" + comboInnerSkinSize.Text + "',"
                                    + "InnerTopExtra='" + textInnerTopExtra.Text + "',"
                                    + "InnerBottomExtra='" + textInnerBottomExtra.Text + "',"
                                    + "InnerTopDia='" + comboInnerTopDia.Text + "',"
                                    + "InnerBottomDia='" + comboInnerBottomDia.Text + "',"
                                    + "InnerTopHeight='" + comboInnerTopHeight.Text + "',"
                                    + "InnerBottomheight='" + comboInnerBottomheight.Text + "',"
                                    + "LoopProtectorSize='" + comboLoopProtector.Text + "',"
                                    + "IsMFWebTop='" + (checkMFWebTop.Checked ? "Yes" : "No") + "',"
                                    + "IsMFWebBottom='" + (checkMFWebBottom.Checked ? "Yes" : "No") + "',"
                                    + "IsMFWebTopSpout='" + (checkMFWebTopSpout.Checked ? "Yes" : "No") + "',"
                                    + "IsMFWebBottomSpout='" + (checkMFWebBottomSpout.Checked ? "Yes" : "No") + "',"
                                    + "IsMFWebBody='" + (checkMFWebBody.Checked ? "Yes" : "No") + "',"
                                    + "BottomFlapCutLenght='" + textBottomFlapCutLenght.Text + "',"
                                    + "ISTopFlapDRing='" + (chkTopFlapDRing.Checked ? "Yes" : "No") + "',"
                                    + "BottomBoxtopflapdring='" + comboBoxtopflapdring.Text + "',"
                                    + "IsBottomFlapDRing='" + (chkBottomFlapDRing.Checked ? "Yes" : "No") + "',"
                                    + "Boxbottomflapdring='" + comboBoxbottomflapdring.Text + "',"
                                    + "SpoutRopeType='" + comboSpoutRope.Text + "'"
                                    + " WHERE SRNO='" + SrNo + "' AND PONO='" + textFilePONo.Text + "'";
                            Database.GetExecuteNonQueryCommand(strupdate);
                            //23.09.2021
                            strupdate = "UPDATE BOM3 SET "
                               + " IsExtraLabel='" + (checkExtraLabel.Checked ? "Yes" : "No") + "',"
                               + " ExtraLabelNo='" + textExtraLabelNo.Text + "',"
                               + " ExtralabelL='" + textExtralabelL.Text + "',"
                               + " ExtralabelW='" + textExtralabelW.Text + "',"
                               + " ExtraLabelMicron='" + comboExtraLabelMicron.Text + "',"
                               + " IsBoXExtraLabel='" + (checkBoXExtraLabel.Checked ? "Yes" : "No") + "',"
                               + " ExtraLabelLam='" + comboExtraLabelLam.Text + "',"
                               + " ExtraLabeltype='" + comboExtraLabeltype.Text + "',"
                               + " Extralabelsubtype='" + comboExtralabelsubtype.Text + "',"
                               + " ExtralabelL1='" + textExtralabelL1.Text + "',"
                               + " ExtralabelW1='" + textExtralabelW1.Text + "',"
                               + " ExtraLabelMicron1='" + comboExtraLabelMicron1.Text + "',"
                               + " IsBoXExtraLabel1='" + (checkBoXExtraLabel1.Checked ? "Yes" : "No") + "',"
                               + " ExtraLabelLam1='" + comboExtraLabelLam1.Text + "',"
                               + " ExtraLabeltype1='" + comboExtraLabeltype1.Text + "',"
                               + " Extralabelsubtype1='" + comboExtralabelsubtype1.Text + "',"
                               + " ExtralabelL2='" + textExtralabelL2.Text + "',"
                               + " ExtralabelW2='" + textExtralabelW2.Text + "',"
                               + " ExtraLabelMicron2='" + comboExtraLabelMicron2.Text + "',"
                               + " IsBoXExtraLabel2='" + (checkBoXExtraLabel2.Checked ? "Yes" : "No") + "',"
                               + " ExtraLabelLam2='" + comboExtraLabelLam2.Text + "',"
                               + " ExtraLabeltype2='" + comboExtraLabeltype2.Text + "',"
                               + " Extralabelsubtype2='" + comboExtralabelsubtype2.Text + "',"
                               + " ExtralabelL3='" + textExtralabelL3.Text + "',"
                               + " ExtralabelW3='" + textExtralabelW3.Text + "',"
                               + " ExtraLabelMicron3='" + comboExtraLabelMicron3.Text + "',"
                               + " IsBoXExtraLabel3='" + (checkBoXExtraLabel3.Checked ? "Yes" : "No") + "',"
                               + " ExtraLabelLam3='" + comboExtraLabelLam3.Text + "',"
                               + " ExtraLabeltype3='" + comboExtraLabeltype3.Text + "',"
                               + " Extralabelsubtype3='" + comboExtralabelsubtype3.Text + "'"
                               + " WHERE SRNO='" + SrNo + "' AND PONO='" + textFilePONo.Text + "'";
                            Database.GetExecuteNonQueryCommand(strupdate);

                            //29.09.2021

                            strupdate = "UPDATE BOM3 SET "
                              + " BottomSpoutRopeNo='" + textBottomSpoutRopeNo.Text + "',"
                              + " BottomSpoutTieNo='" + textBottomSpoutTieNo.Text + "',"
                              + " Boxlabelnos='" + textBoxlabelnos.Text + "',"
                              + " TopHookNo='" + textTopHookNo.Text + "',"
                              + " BottomHookNo='" + textBottomHookNo.Text + "',"
                              + " NosAncerieLoop='" + textNosAncerieLoop.Text + "',"
                              + " TopTieNo='" + textTopTieNo.Text + "',"
                              + " TopRopeNo='" + textTopRopeNo.Text + "',"
                              + " BottomTieNo='" + textBottomTieNo.Text + "',"
                              + " BottomRopeNo='" + textBottomRopeNo.Text + "',"
                              + " TopSpoutTieIRISNo='" + textTopSpoutTieIRISNo.Text + "',"
                              + " BottomSpoutTieIRISNo='" + textBottomSpoutTieIRISNo.Text + "',"
                              + " LoopCoverNo='" + textLoopCoverNo.Text + "',"
                              + " StNo='" + textStNo.Text + "',"
                              + " TopSpoutRopeNo='" + textTopSpoutRopeNo.Text + "',"
                              + " TopSpoutTieNo='" + textTopSpoutTieNo.Text + "'"
                              + " WHERE SRNO='" + SrNo + "' AND PONO='" + textFilePONo.Text + "'";
                            Database.GetExecuteNonQueryCommand(strupdate);

                            //07.10.2021
                            strupdate = "UPDATE BOM3 SET "
                             + " TopTieCutSizes='" + comboTopTieCutSizes.Text + "',"
                             + " TopRopeCutSizes='" + comboTopRopeCutSizes.Text + "',"
                             + " BottomTieCutSize='" + comboBottomTieCutSize.Text + "',"
                             + " BottomRopeCutSizes='" + comboBottomRopeCutSizes.Text + "'"
                             + " WHERE SRNO='" + SrNo + "' AND PONO='" + textFilePONo.Text + "'";
                            Database.GetExecuteNonQueryCommand(strupdate);


                            strupdate = "UPDATE BOM3 SET "
                             + " fillercordtoptype='" + comboFillerCordTop.Text + "',"
                             + " fillercordbottomtype='" + comboFillerCordBottom.Text + "',"
                             + " fillercordFSType='" + comboFillerCordTopS.Text + "',"
                             + " FillercordDStype='" + comboFillerCordBottomS.Text + "',"
                             + " fillercordbodytype='" + comboFillerCordBody.Text + "',"
                             + " fillercordbuffletype='" + comboFillerCordBuffle.Text + "',"
                             + " fillercordbuffle1='" + (checkFillerBuffle.Checked ? "yes":"no") + "'"
                             + " WHERE SRNO='" + SrNo + "' AND PONO='" + textFilePONo.Text + "'";
                            Database.GetExecuteNonQueryCommand(strupdate);


                            strupdate = "UPDATE BOM3 SET "
                          + " fsedgehaming='" + (checkTopEdgeHemming.Checked ? "yes" : "no") + "',"
                          + " dsedgehaming='" + (checkBottomEdgeHemming.Checked ? "yes" : "no") + "',"
                          + " docflap='" + (checkDocFlap.Checked ? "yes" : "no") + "',"
                          + " docflapsize='" + textDocFlapsize.Text + "',"
                          + " mfwebbingbuffle='" + (checkMFBuffle.Checked ? "yes" : "no") + "'"
                       
                          + " WHERE SRNO='" + SrNo + "' AND PONO='" + textFilePONo.Text + "'";
                            Database.GetExecuteNonQueryCommand(strupdate);



                           
                            //end 
                            #endregion

                        }

                        IsTemp = true;
                        if (!IsError)
                        {
                            if (comboLoopL.Text == "")
                                comboLoopL.Text = "0";
                            DataColumn dc = new DataColumn("TotalKG", typeof(Double));
                            dc.DefaultValue = TotalKg;
                            DataColumn dc1 = new DataColumn("ShortLen", typeof(Double));
                            dc1.DefaultValue = textShortLeg.Text;
                            DataColumn dc2 = new DataColumn("LoopL", typeof(Double));
                            dc2.DefaultValue = comboLoopL.Text;

                            DataColumn dc3 = new DataColumn("IsDropLoop", typeof(string));
                            dc3.DefaultValue = (checkBoxDropLoop.Checked ? "yes" : "no");

                            DataColumn dc4 = new DataColumn("DropLoop", typeof(int));
                            dc4.DefaultValue = textLoopDropLenght.Text;

                            DataColumn dc5 = new DataColumn("Instruction", typeof(string));
                            dc5.DefaultValue = textInstruction.Text;

                            DataColumn dc6 = new DataColumn("printingremarks", typeof(string));
                            dc6.DefaultValue = textprintingremarks.Text;

                            DataColumn dc7 = new DataColumn("looplongleg", typeof(string));
                            dc7.DefaultValue = textLongLeg.Text;

                            DataColumn dc8 = new DataColumn("ApprovalField", typeof(string));
                            dc8.DefaultValue = Approvalfield;

                            DataColumn dc9 = new DataColumn("KnotType", typeof(string));
                            dc9.DefaultValue = comboBottomKnotType.Text;

                            dataset2.Tables[0].Columns.Add(dc);
                            dataset2.Tables[0].Columns.Add(dc1);
                            dataset2.Tables[0].Columns.Add(dc2);
                            dataset2.Tables[0].Columns.Add(dc3);
                            dataset2.Tables[0].Columns.Add(dc4);
                            dataset2.Tables[0].Columns.Add(dc5);
                            dataset2.Tables[0].Columns.Add(dc6);
                            dataset2.Tables[0].Columns.Add(dc7);
                            dataset2.Tables[0].Columns.Add(dc8);
                            dataset2.Tables[0].Columns.Add(dc9);

                            dataset1.Tables[0].Merge(dataset2.Tables[0]);
                            bool isDorpLoomTemp = checkBoxDropLoop.Checked;

                            if (IsupdateMode && btnupdateclick)
                            {
                                Database.CommitTransaction();
                                btnupdateclick = false;
                                Utility.UserInformation("Update ", FrmMainForm.UserName, "BOM No with Quotation No " + textFilePONo.Text + " updated successfully");
                                MessageBox.Show("Data Updated Succseefully");
                            }
                            frmPrintBillOfMaterial frm = new frmPrintBillOfMaterial(dataset1.Tables[0].Copy(), isDorpLoomTemp, true);
                            frm.WindowState = FormWindowState.Maximized;
                            frm.ShowDialog();
                        }

                    }
                    catch (Exception ex)
                    {
                        if (IsupdateMode && btnupdateclick)
                            Database.RollBackTransaction();
                        MessageBox.Show(ex.ToString());
                    }

                }
                Database.Closeconnection();

            }

        }

        private void comboSF_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboLoopGrm.Text = "0";
            if (textSWL.Text != "")
            {
                double SWL = Utility.SafeConvertToDouble(textSWL.Text);
                if (_BodyIndex1 == 0 || _BodyIndex1 == 2
                    || _BodyIndex1 == 3 || _BodyIndex1 == 4
                    || _BodyIndex1 == 9 || _BodyIndex1 == 10)
                {
                    if (comboBody2.SelectedIndex == 4)
                    {
                        if (comboSF.SelectedIndex == 0) // 5:1
                        {
                            if (SWL <= 500)
                                comboBodyGSM.Text = "110";
                            else if (SWL <= 1000)
                                comboBodyGSM.Text = "130";
                            else if (SWL <= 1250)
                                comboBodyGSM.Text = "150";
                            else if (SWL <= 1500)
                                comboBodyGSM.Text = "170";
                            else if (SWL <= 2000)
                                comboBodyGSM.Text = "190";
                        }
                    }
                    else
                    {
                        if (comboSF.SelectedIndex == 0) // 5:1
                        {
                            if (SWL <= 500)
                                comboBodyGSM.Text = "120";
                            else if (SWL <= 1000)
                                comboBodyGSM.Text = "140";
                            else if (SWL <= 1250)
                                comboBodyGSM.Text = "160";
                            else if (SWL <= 1500)
                                comboBodyGSM.Text = "180";
                            else if (SWL <= 2000)
                                comboBodyGSM.Text = "200";
                        }
                    }

                }
                else if (_BodyIndex1 == 1 || _BodyIndex1 == 12 || _BodyIndex1 == 13)
                {
                    if (comboSF.SelectedIndex == 0) // 5:1
                    {
                        if (SWL <= 1000)
                            comboBodyGSM.Text = "140";
                        else if (SWL <= 1500)
                            comboBodyGSM.Text = "160";
                        else if (SWL <= 2000)
                            comboBodyGSM.Text = "200";
                    }
                }
                else if (_BodyIndex1 == 5 || _BodyIndex1 == 7)
                {
                    if (comboSF.SelectedIndex == 0) // 5:1
                    {
                        if (SWL <= 1000)
                            comboBodyGSM.Text = "140";
                        else if (SWL <= 1500)
                            comboBodyGSM.Text = "160";
                        else if (SWL <= 2000)
                            comboBodyGSM.Text = "200";
                    }
                }
                else if (_BodyIndex1 == 6 || _BodyIndex1 == 8)
                {
                    if (comboSF.SelectedIndex == 0) // 5:1
                    {
                        if (SWL <= 2000)
                            comboBodyGSM.Text = "140";
                    }
                }
            }
        }

        private void checkBottomTie_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomTie.Checked)
                groupBottomTie.Visible = true;
            else
                groupBottomTie.Visible = false;

        }

        private void checkTopTie_CheckedChanged(object sender, EventArgs e)
        {
            if (checkTopTie.Checked)
                groupTopTie.Visible = true;
            else
                groupTopTie.Visible = false;
        }

        private void checkBoxLam_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLam.Checked)
            {
                comboBodyLamiGSM.Visible = true;
                //By Rikin 
                if (checkSide.Checked)
                {
                    checkSideLami.Checked = true;
                }
                // till here 
                comboBodyLamiGSM.Text = "30";

                if ((_BodyIndex1 == 0 || _BodyIndex1 == 2 || comboBody3.SelectedIndex == 3)  && IsupdateMode == false)
                {
                    comboBoxbottomgsm.Text = comboBodyGSM.Text;
                    checkBoxbottomlam.Checked = true;
                    comboBottomLamiGSM.Text = comboBodyLamiGSM.Text;
                }
                else if (_BodyIndex1 == 1 && IsupdateMode == false) //Circular
                {
                    comboBoxbottomgsm.Text = Convert.ToString(_BagGSM + 10);
                    checkBoxbottomlam.Checked = true;
                    comboBottomLamiGSM.Text = comboBodyLamiGSM.Text;
                }
                else
                {
                    checkBoxbottomlam.Checked = false;
                    comboBottomLamiGSM.Text = "0";
                }

            }
            else
            {
                comboBodyLamiGSM.Visible = false;
                comboBodyLamiGSM.Text = "0";
                //By Rikin 
                checkSideLami.Checked = false;
                // comboSideGSM.Text = "0";
                // till here 

            }
        }

        private void checkBoxSpoutLam_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSpoutLam.Checked)
            {
                comboSpoutLamiGSM.Visible = true;
                comboSpoutLamiGSM.Text = "25";
            }
            else
            {
                comboSpoutLamiGSM.Visible = false;
                comboSpoutLamiGSM.Text = "0";
            }
        }

        private void checkBoxbottomlam_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxbottomlam.Checked)
            {
                comboBottomLamiGSM.Visible = true;
                comboBottomLamiGSM.Text = "25";
            }
            else
            {
                comboBottomLamiGSM.Visible = false;
                comboBottomLamiGSM.Text = "0";
            }
        }

        private void checkBoxbottomlam1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxbottomlam1.Checked)
            {
                comboBoxBottomSubTypeLamiGSM.Visible = true;
                comboBoxBottomSubTypeLamiGSM.Text = "25";
            }
            else
            {
                comboBoxBottomSubTypeLamiGSM.Visible = false;
                comboBoxBottomSubTypeLamiGSM.Text = "0";
            }
        }

        private void checkBoxTopLam_CheckedChanged(object sender, EventArgs e)
        {

            //if(comboBoxTopGSM.Text == "0")
            //    comboTopLamiGSM.Text = "0";
            if (checkBoxTopLam.Checked)
            {
                comboTopLamiGSM.Visible = true;
                comboTopLamiGSM.Text = "25";
            }
            else
            {
                comboTopLamiGSM.Visible = false;
                comboTopLamiGSM.Text = "0";
            }
        }

        private void comboDocType2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboDocType2.SelectedIndex == 0) //A1
            {

                textDocL.Text = "23.6";
                textDocW.Text = "33.32";
                comboDocUnit.SelectedIndex = 1;

            }
            if (comboDocType2.SelectedIndex == 1) //A2
            {
                textDocL.Text = "16.66";
                textDocW.Text = "23.6";
                comboDocUnit.SelectedIndex = 1;

            }
            if (comboDocType2.SelectedIndex == 2) //A3
            {

                textDocL.Text = "11.8";
                textDocW.Text = "16.66";
                comboDocUnit.SelectedIndex = 1;

            }
            if (comboDocType2.SelectedIndex == 3) //A4
            {

                textDocL.Text = "8.33";
                textDocW.Text = "11.8";
                comboDocUnit.SelectedIndex = 1;

            }
            if (comboDocType2.SelectedIndex == 4) //A5
            {

                textDocL.Text = "5.9";
                textDocW.Text = "8.33";
                comboDocUnit.SelectedIndex = 1;

            }
            if (comboDocType2.SelectedIndex == 5) //A6
            {
                textDocL.Text = "4.16";
                textDocW.Text = "5.9";
                comboDocUnit.SelectedIndex = 1;

            }

            if (comboDocUnit.SelectedIndex == 0)
            {

            }

        }

        private void checkBoxdocpouch_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBoxdocpouch.Checked)
            {
                groupBoxdocpouch.Visible = true;
                if (IsupdateMode == false)
                {
                    comboDocType.SelectedIndex = 0;
                    comboDocType1.SelectedIndex = 0;
                    comboDocType2.SelectedIndex = 0;
                    comboDocMicron.Text = "80";

                }
            }
            else
                groupBoxdocpouch.Visible = false;
        }

        private void checktopflap_CheckedChanged(object sender, EventArgs e)
        {
            if (checktopflap.Checked)
                groupBoxtopflap.Visible = true;
            else
                groupBoxtopflap.Visible = false;
        }

        private void checkTopFlapHook_CheckedChanged(object sender, EventArgs e)
        {
            if (checkTopFlapHook.Checked)
                groupTopHook.Visible = true;
            else
                groupTopHook.Visible = false;
        }

        private void checkBottomflap_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomflap.Checked)
                groupBoxbottomflap.Visible = true;
            else
                groupBoxbottomflap.Visible = false;
        }

        private void checkBottomFlapHook_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomFlapHook.Checked)
                groupBottomHook.Visible = true;
            else
                groupBottomHook.Visible = false;
        }

        private void checkTopTie_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkTopTie.Checked)
                groupTopTie.Visible = true;
            else
                groupTopTie.Visible = false;
        }

        private void checkTopRope_CheckedChanged(object sender, EventArgs e)
        {
            if (checkTopRope.Checked)
                groupTopRope.Visible = true;
            else
                groupTopRope.Visible = false;
        }

        private void checkBottomTie_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBottomTie.Checked)
                groupBottomTie.Visible = true;
            else
                groupBottomTie.Visible = false;
        }

        private void checkBottomRope_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomRope.Checked)
                groupBottomRope.Visible = true;
            else
                groupBottomRope.Visible = false;
        }

        private void checkBoxDropLoop_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDropLoop.Checked)
                groupDropLoop.Visible = true;
            else
                groupDropLoop.Visible = false;
        }

        private void checkFillerCord_CheckedChanged(object sender, EventArgs e)
        {
            if (checkFillerCord.Checked)
                groupFillerCord.Visible = true;
            else
                groupFillerCord.Visible = false;
        }

        private void checkSpoutRope_CheckedChanged(object sender, EventArgs e)
        {
            if (checkSpoutRope.Checked)
            {
                groupTopSpoutRope.Visible = true;
                comboSpoutRope.SelectedIndex = 0;
                comboTopSpoutRopeGrm.Text = "8";
                comboSpoutRopeSize.Text = "6";
            }
            else
                groupTopSpoutRope.Visible = false;
        }

        private void checkSpoutTie_CheckedChanged(object sender, EventArgs e)
        {
            if (checkSpoutTie.Checked)
            {
                groupTopSpoutTie.Visible = true;
                textTopSpoutTieRemarks.Text = "Size: " + comboSpoutTieCutSize.Text;
            }
            else
                groupTopSpoutTie.Visible = false;
        }

        private void checkBottomSpoutRope_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomSpoutRope.Checked)
            {
                groupBottomSpoutRope.Visible = true;
                comboBottomSpoutRopeGrm.Text = "8";
                comboBottomSpoutRopeSize.Text = "6";

            }
            else
                groupBottomSpoutRope.Visible = false;
        }

        private void checkBottomSpoutTie_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomSpoutTie.Checked)
            {
                groupBottomSpoutTie.Visible = true;
                textBottomSpoutTieRemarks.Text = "Size: " + comboBottomSpoutTieCutSize.Text;
            }
            else
                groupBottomSpoutTie.Visible = false;
        }

        private void checkLoopCover_CheckedChanged(object sender, EventArgs e)
        {
            if (checkLoopCover.Checked)
                groupLoopCover.Visible = true;
            else
                groupLoopCover.Visible = false;
        }

        private void checkSide_CheckedChanged(object sender, EventArgs e)
        {
            if (checkSide.Checked)
                groupSide.Visible = true;
            else
                groupSide.Visible = false;

            if (_BodyIndex1 == 0)
                comboSideGSM.Text = comboBodyGSM.Text;

            string remarks = "";
            if (_BodyIndex1 == 2)
            {
                if (comboType.SelectedIndex == 0 && comboBody2.SelectedIndex == 7) // Internal
                {
                    double x1 = Math.Round(_BagWidth / 3, 1);
                    double y = x1 + 2;
                    remarks = "Buffle Coding " + y.ToString() + "," + x1.ToString() + "," + y.ToString();
                }
                else if (comboType.SelectedIndex == 0 && comboBody2.SelectedIndex != 7) // Internal
                {
                    double x1 = Math.Round(_BagWidth / 3, 1);
                    double y = x1 + 5.5;
                    remarks = "Buffle Coding " + y.ToString() + "," + x1.ToString() + "," + y.ToString();
                }
                textSideRemarks.Text = remarks;
                //Buffle Coding 35.5,30,35.5 nonsuzler
            }
        }

        private void checkLoopCoverLami_CheckedChanged(object sender, EventArgs e)
        {
            if (checkLoopCoverLami.Checked)
                comboLoopCoverLamiGSM.Visible = true;
            else
            {
                comboLoopCoverLamiGSM.Visible = false;
                comboLoopCoverLamiGSM.Text = "0";
            }
        }

        private void checkTunnelLam_CheckedChanged(object sender, EventArgs e)
        {
            if (checkTunnelLam.Checked)
                comboTunnelLamiGSM.Visible = true;
            else
            {
                comboTunnelLamiGSM.Visible = false;
                comboTunnelLamiGSM.Text = "0";
            }
        }


        private void checkTopFlapLami_CheckedChanged(object sender, EventArgs e)
        {
            if (checkTopFlapLami.Checked)
                comboTopflapLamiGsm.Visible = true;
            else
            {
                comboTopflapLamiGsm.Visible = false;
                comboTopflapLamiGsm.Text = "0";
            }
        }

        private void checkBottomFlapLami_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomFlapLami.Checked)
                comboBottomflapLamiGSM.Visible = true;
            else
            {
                comboBottomflapLamiGSM.Visible = false;
                comboBottomflapLamiGSM.Text = "0";
            }
        }

        private void checkSideLami_CheckedChanged(object sender, EventArgs e)
        {
            if (checkSideLami.Checked)
            {
                comboSideLamiGSM.Visible = true;

            }
            else
            {
                comboSideLamiGSM.Visible = false;
                comboSideLamiGSM.Text = "0";
            }
        }

        private void checkInnerBox_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInnerBox.Checked)
                groupInnerBox.Visible = true;
            else
                groupInnerBox.Visible = false;
        }

        private void checkInnerBoxLami_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInnerBoxLami.Checked)
                comboInnerBoxLamiGSM.Visible = true;
            else
            {
                comboInnerBoxLamiGSM.Visible = false;
                comboInnerBoxLamiGSM.Text = "0";
            }
        }

        private void btnInit_Click(object sender, EventArgs e)
        {
            IsupdateMode = false;
            comboBodyLamiGSM.Text = "0";
            comboSideLamiGSM.Text = "0";
            comboSpoutLamiGSM.Text = "0";
            comboTopflapLamiGsm.Text = "0";
            comboLoopCoverLamiGSM.Text = "0";
            comboInnerBoxLamiGSM.Text = "0";
            comboBoxBottomSubTypeLamiGSM.Text = "0";
            comboBagType.Text = "";
            textBodyL.Text = "";
            textBodyW.Text = "";
            textBodyH.Text = "";
            textSWL.Text = "";
            textQty.Text = "";
            textFilePONo.Text = "";
            comboPartyName.Text = "";
            textBodyRemarks.Text = "";
            comboBagType.Text = "Type A";

            textInstruction.Text = "";
            textpono.Text = "";

            checkSide.Checked = false;
            checkSideLami.Checked = false;
            checkBoxTopBand.Checked = false;
            checkFillerCord.Checked = false;
            checkLoopProtector.Checked = false;

            comboBodyLamiGSM.Visible = false;
            comboSpoutLamiGSM.Visible = false;
            comboBottomLamiGSM.Visible = false;
            comboTopLamiGSM.Visible = false;
            comboBoxBottomSubTypeLamiGSM.Visible = false;
            comboLoopCoverLamiGSM.Visible = false;
            comboTunnelLamiGSM.Visible = false;
            comboTopflapLamiGsm.Visible = false;
            comboBottomflapLamiGSM.Visible = false;
            comboSideLamiGSM.Visible = false;
            comboInnerBoxLamiGSM.Visible = false;
            comboInnerBottomLamiGSM.Visible = false;
            comboInnerTopLamiGSM.Visible = false;
            comboInnerSkinLamiGSM.Visible = false;
            comboLoopProctectorLamiGSM.Visible = false;

            groupTopTie.Visible = false;
            groupBottomTie.Visible = false;
            groupBoxstevdore.Visible = false;
            groupSpout.Visible = false;
            groupBox6.Visible = false;
            groupBoxduffleskirt.Visible = false;
            groupTunnel.Visible = false;
            groupTop.Visible = false;
            groupBoxtopband.Visible = false;
            groupBoxtopflap.Visible = false;
            groupboxbottom.Visible = false;
            groupBoxbottom2.Visible = false;
            groupBoxbottom1.Visible = false;
            groupBoxbottomflap.Visible = false;
            groupBoxliner.Visible = false;
            groupBoxlabel.Visible = false;
            groupBoxblock.Visible = false;
            groupBoxdocpouch.Visible = false;
            groupBuffle.Visible = false;
            groupSingleLoop.Visible = false;
            groupTopSpoutRope.Visible = false;
            groupTopSpoutTie.Visible = false;
            groupBottomSpoutRope.Visible = false;
            groupBottomSpoutTie.Visible = false;
            groupTopHook.Visible = false;
            groupBottomHook.Visible = false;
            groupTopRope.Visible = false;
            groupBottomRope.Visible = false;
            groupFillerCord.Visible = false;
            groupDropLoop.Visible = false;
            groupLoopCover.Visible = false;
            groupSide.Visible = false;
            groupInnerBox.Visible = false;
            groupInnerSkin.Visible = false;
            groupInnerTop.Visible = false;
            groupInnerBottom.Visible = false;
            groupLoops.Visible = false;
            groupLoopProc.Visible = false;
            groupBuffleSeam.Visible = false;
            groupThread.Visible = false;
            //groupHiracle.Visible = false;
            groupBoxtopBellyband1.Visible = false;
            groupBoxtopBellyband2.Visible = false;
            groupBoxtopBottomBand.Visible = false;
            groupStevedorecover.Visible = false;

            checkSide.Checked = false;
            checkStevedorecover.Checked = false;
            checkLoopCover.Checked = false;
            checkLoopCoverLami.Checked = false;
            checkbottom.Checked = false;
            checkBottomflap.Checked = false;
            checktopflap.Checked = false;
            checkBoxLoop.Checked = false;
            checkTunnelLam.Checked = false;
            checkBoxbottomcabletie.Checked = false;
            checkBoxbottomlam.Checked = false;
            checkbottomvelcro.Checked = false;
            checkBoxdocpouch.Checked = false;
            checkBoxDropLoop.Checked = false;
            checkBoxlabel.Checked = false;
            checkBoxLam.Checked = false;
            checkBoxTunnel.Checked = false;
            checkStevdore.Checked = false;
            checkBoxTop.Checked = false;
            checkSpoutRope.Checked = false;
            checkSpoutTie.Checked = false;
            checkThread.Checked = false;
            checkBottomSpoutRope.Checked = false;
            checkBottomSpoutTie.Checked = false;
            checkBottomTie.Checked = false;
            checkBottomRope.Checked = false;
            checkInnerBottom.Checked = false;
            checkInnerBottomLami.Checked = false;
            checkInnerBox.Checked = false;
            checkInnerBoxLami.Checked = false;
            checkInnerSkinLami.Checked = false;
            checkInnerSkin.Checked = false;
            checkTopFlapHook.Checked = false;
            checkTopFlapLami.Checked = false;
            checkBottomFlapHook.Checked = false;
            checkTopTie.Checked = false;
            checkTopRope.Checked = false;
            checkBoxliner.Checked = false;
            checkSide.Checked = false;
            checkBoxlabel.Checked = true;
            checkBoxTopLam.Checked = false;

            if (checkBoxLoop.Checked == false)
                comboLoopConst.SelectedIndex = 0;
            comboLoopType.SelectedIndex = 0;
            comboLoopProtector.SelectedIndex = 0;

            comboTopType.SelectedIndex = 0;
            _Type = 0;
            _BodyIndex1 = 0;
            comboBodyGSM.SelectedIndex = 0;
            comboBodyUnit.SelectedIndex = 0;
            comboBody2.SelectedIndex = 0;
            comboBody3.SelectedIndex = 0;
            comboBoxbottomdia.SelectedIndex = 0;
            comboBoxbottomgsm.SelectedIndex = 0;
            comboBoxbottomgsm1.SelectedIndex = 0;
            comboBoxbottomheight.SelectedIndex = 0;
            comboBottomhoseslider.SelectedIndex = 0;
            combobottomvelcro.SelectedIndex = 0;
            comboBoxduffleskirtheight.SelectedIndex = 0;
            comboTopHoseSlider.SelectedIndex = 0;
            comboBoxlineratpoint.SelectedIndex = 0;
            comboBoxlinerheight.SelectedIndex = 0;
            comboBoxlinermicron.SelectedIndex = 0;
            comboBoxlinertype.SelectedIndex = 0;
            comboBoxlinertype1.SelectedIndex = 0;
            comboBoxlinerwidth.SelectedIndex = 0;
            comboBoxpacking.SelectedIndex = 0;
            comboBoxtopflapdring.SelectedIndex = 0;
            comboBoxtopflapgsm.SelectedIndex = 0;
            comboBoxtransport.SelectedIndex = 0;
            comboTopVelcro.SelectedIndex = 0;
            comboBuffleGSM.SelectedIndex = 0;
            comboLoopConst.SelectedIndex = 0;
            comboLoopGrm.SelectedIndex = 0;
            comboLoopL.SelectedIndex = 0;
            comboLoopProtector.SelectedIndex = 0;
            comboLoopType.SelectedIndex = 0;
            comboLoopW.SelectedIndex = 0;
            comboSF.SelectedIndex = 0;
            comboSpoutGSM.SelectedIndex = 0;
            comboSpoutType.SelectedIndex = 0;
            comboStGrm.SelectedIndex = 0;
            comboStSize.SelectedIndex = 0;
            comboSWLUnit.SelectedIndex = 1;
            combotopbandgrm.SelectedIndex = 0;
            comboTopType.SelectedIndex = 0;
            comboTunnelGSM.SelectedIndex = 0;
            comboTunnelLen.SelectedIndex = 0;
            comboTunnelWid.SelectedIndex = 0;
            comboBodyLamiGSM.SelectedIndex = 0;
            comboSpoutLamiGSM.SelectedIndex = 0;
            comboBoxTopGSM.SelectedIndex = 0;
            comboTopLamiGSM.SelectedIndex = 0;
            comboSpoutDia.SelectedIndex = 0;
            comboSpoutHeight.SelectedIndex = 0;
            comboBoxbottomtype.SelectedIndex = 0;
            comboBoxBottomSubTypeLamiGSM.SelectedIndex = 0;
            comboBottomLamiGSM.SelectedIndex = 0;
            comboBoxbottomsubtype.SelectedIndex = 5;
            btnUpdate.Enabled = false;
            btnSave.Enabled = true;
            textFilePONo.Enabled = true;
            if (_BodyIndex1 == 0)
                checkSide.Checked = true;
            else
                checkSide.Checked = false;
            checkBoxTopLam.Checked = false;

            textPallets.Text = ".10";
            textBLock.Text = ".20";
            textHoseSlider.Text = ".05";
            textVelcro.Text = ".20";
            textDustProof.Text = "3.5";
            textFelt.Text = "8";
            checkOrderConfirmed.Checked = false;
            checkBoxRF.Checked = false;

            FilePONo = string.Empty;
            chkFabricPatch.Checked = false;
            chkFabricPatch.Visible = false;
            dataGridView1.Rows.Clear();

            // added as per requirement of Quality 19th July 2022
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[0].Cells[0].Value = "Top Spout Velcro";
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[1].Cells[0].Value = "Top Flap Velcro";
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[2].Cells[0].Value = "Bottom Spout Velcro";
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[3].Cells[0].Value = "Bottom Flap Velcro";

            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[4].Cells[0].Value = "FS B Lock";
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[5].Cells[0].Value = "DS B Lock";
            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[6].Cells[0].Value = "Petal/Iris B Lock";

            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[7].Cells[0].Value = "DS Elastic band";

            //dataGridView1.Rows.Add();
            //dataGridView1.Rows[8].Cells[0].Value = "Petal/Iris Elastic band";

            // end  as per requirement of Quality 19th July 2022

            for(int i = 0 ;i<10;i++)
            {
                dgapprovallist.Rows[i].Cells[0].Value = "false";
            }

            ClearVariables();

        }

        private void checkLoopProtector_CheckedChanged(object sender, EventArgs e)
        {
            if (checkLoopProtector.Checked)
                groupLoopProc.Visible = true;
            else
                groupLoopProc.Visible = false;
        }

        private void comboLoopProtector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboLoopProtector.SelectedIndex == 1)//Wedding
            {
                checkLoopProcLami.Visible = false;
                label172.Text = "Grm";
            }
            else if (comboLoopProtector.SelectedIndex == 2)
            {
                checkLoopProcLami.Visible = true;
                label172.Text = "GSM";
            }
        }

        private void checkLoopProcLami_CheckedChanged(object sender, EventArgs e)
        {
            if (checkLoopProcLami.Checked)
                comboLoopProctectorLamiGSM.Visible = true;
            else
            {
                comboLoopProctectorLamiGSM.Visible = false;
                comboLoopProctectorLamiGSM.Text = "0";
            }
        }

        private void checkInnerSkin_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInnerSkin.Checked)
                groupInnerSkin.Visible = true;
            else
                groupInnerSkin.Visible = false;
        }

        private void checkInnerTop_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInnerTop.Checked)
                groupInnerTop.Visible = true;
            else
                groupInnerTop.Visible = false;
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInnerBottom.Checked)
                groupInnerBottom.Visible = true;
            else
                groupInnerBottom.Visible = false;
        }

        private void checkInnerSkinLami_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInnerSkinLami.Checked)
                comboInnerSkinLamiGSM.Visible = true;
            else
            {
                comboInnerSkinLamiGSM.Visible = false;
                comboInnerSkinLamiGSM.Text = "0";
            }
        }

        private void checkInnerTopLami_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInnerTopLami.Checked)
                comboInnerTopLamiGSM.Visible = true;
            else
            {
                comboInnerTopLamiGSM.Visible = false;
                comboInnerTopLamiGSM.Text = "0";
            }
        }

        private void checkInnerBottomLami_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInnerBottomLami.Checked)
                comboInnerBottomLamiGSM.Visible = true;
            else
            {
                //comboInnerSkinLamiGSM.Visible = false;
                //comboInnerSkinLamiGSM.Text = "0";

                comboInnerBottomLamiGSM.Visible = false;
                comboInnerBottomLamiGSM.Text = "0";

            }
        }

        private void checkBoxLoop_CheckedChanged(object sender, EventArgs e)
        {

            if (comboLoopConst.SelectedIndex != 2)//Not a Cross corner loop
            {
                if (!IsupdateMode)
                {


                    if (comboBody2.SelectedIndex == 0 || comboBody2.SelectedIndex == 6 || comboBody2.SelectedIndex == 7) //Non-Bulder
                        textShortLeg.Text = "50";
                    else if (comboBody2.SelectedIndex == 4) //Builder
                        textShortLeg.Text = "40";
                }
            }

            if (checkBoxLoop.Checked)
            {
                groupLoops.Visible = true;
                if (!IsupdateMode)
                {
                    textLoopLenght.Text = "";
                    LoopGRMTable();

                    comboLoopL.Text = "30";
                }
            }
            else
            {
                groupLoops.Visible = false;
                if (!IsupdateMode)
                    comboLoopL.Text = "30";
            }

            if (!IsupdateMode)
            {

                if (_BodyIndex1 == 1) //Circular
                {
                    comboLoopConst.SelectedIndex = 2;
                    comboLoopW.Text = "7";
                }
                if (_BodyIndex1 == 0 || _BodyIndex1 == 2 || _BodyIndex1 == 3)
                {
                    if (comboBuffleType.SelectedIndex == 6)
                        comboLoopConst.SelectedIndex = 2;
                    else
                        comboLoopConst.SelectedIndex = 1;
                    if (comboBody2.SelectedIndex == 4 || comboBody2.SelectedIndex == 5) //Builer & Tunnel
                        comboLoopW.Text = "4";
                    else
                        comboLoopW.Text = "5";
                }
            }
        }

        private void checkHiracle_CheckedChanged(object sender, EventArgs e)
        {
            if (checkHiracle.Checked)
                groupHiracle.Visible = true;
            else
                groupHiracle.Visible = false;
        }

        private void checkThread_CheckedChanged(object sender, EventArgs e)
        {
            if (checkThread.Checked)
            {
                if (_BodyIndex1 == 0) //Upanel
                    checkHiracle.Checked = true;
                if (_BodyIndex1 == 2 || _BodyIndex1 == 3) //Buffle
                {
                    comboThreadBuffleSeam.SelectedIndex = 2;
                    checkHiracle.Checked = true;
                    checkHiracleBottom.Checked = true;
                }
                groupThread.Visible = true;
            }
            else
            {
                checkHiracle.Checked = false;
                checkHiracleBottom.Checked = false;
                groupThread.Visible = false;
            }
        }

        private void textSWL_Leave(object sender, EventArgs e)
        {
            SetValue();// by Rikin set value
            if (textSWL.Text == "")
                textSWL.Text = "0";
            if (_BodyIndex1 == 0 || _BodyIndex1 == 2
                || _BodyIndex1 == 3 || _BodyIndex1 == 4
                || _BodyIndex1 == 9 || _BodyIndex1 == 10)
            {
                if (comboBody2.SelectedIndex == 4) //Builder Bag
                {
                    if (comboSF.SelectedIndex == 0) // 5:1
                    {
                        if (Utility.SafeConvertToDouble(textSWL.Text) <= 500)
                        {
                            comboBodyGSM.Text = "110";
                            comboLoopGrm.Text = "25";
                            comboBoxbottomgsm.Text = "110";
                        }
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        {
                            comboBodyGSM.Text = "122";
                            comboLoopGrm.Text = "25";
                            comboBoxbottomgsm.Text = "122";
                        }
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1250)
                            comboBodyGSM.Text = "152";
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                            comboBodyGSM.Text = "172";
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 2000)
                            comboBodyGSM.Text = "192";
                    }
                    else if (comboSF.SelectedIndex == 1)
                    {
                        if (Utility.SafeConvertToDouble(textSWL.Text) <= 500)
                            comboBodyGSM.Text = "132";
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                            comboBodyGSM.Text = "152";
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1250)
                            comboBodyGSM.Text = "172";
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                            comboBodyGSM.Text = "190";
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 2000)
                            comboBodyGSM.Text = "210";
                    }
                }
                else if (comboBody2.SelectedIndex == 5) //Tunnel
                {
                    checkBoxTunnel.Checked = true;
                    comboBodyGSM.Text = "132";
                    comboSideGSM.Text = "122";
                    comboTunnelGSM.Text = "132";
                }
                else // Non-Builder Bag
                {
                    if (comboSF.SelectedIndex == 0) // 5:1
                    {
                        if (Utility.SafeConvertToDouble(textSWL.Text) <= 500)
                        {
                            comboBodyGSM.Text = "122";
                            comboLoopGrm.Text = "32";
                            comboBoxbottomgsm.Text = "122";
                        }
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        {
                            comboBodyGSM.Text = "142";
                            comboLoopGrm.Text = "32";
                            comboBoxbottomgsm.Text = "140";
                        }
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1250)
                        {
                            comboBodyGSM.Text = "162";
                            comboLoopGrm.Text = "42";
                            comboBoxbottomgsm.Text = "160";
                        }
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                        {
                            comboBodyGSM.Text = "182";
                            comboLoopGrm.Text = "52";
                            comboBoxbottomgsm.Text = "180";
                        }
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 2000)
                        {
                            comboBodyGSM.Text = "202";
                            comboLoopGrm.Text = "62";
                            comboBoxbottomgsm.Text = "220";
                        }
                    }
                    else if (comboSF.SelectedIndex == 1) // 6:1
                    {
                        if (Utility.SafeConvertToDouble(textSWL.Text) <= 500)
                        {
                            comboBodyGSM.Text = "142";
                            comboLoopGrm.Text = "32";
                            comboBoxbottomgsm.Text = "142";
                        }
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        {
                            comboBodyGSM.Text = "162";
                            comboLoopGrm.Text = "32";
                            comboBoxbottomgsm.Text = "160";
                        }
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1250)
                        {
                            comboBodyGSM.Text = "182";
                            comboLoopGrm.Text = "42";
                            comboBoxbottomgsm.Text = "180";
                        }
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                        {
                            comboBodyGSM.Text = "202";
                            comboLoopGrm.Text = "52";
                            comboBoxbottomgsm.Text = "200";
                        }
                        else if (Utility.SafeConvertToDouble(textSWL.Text) <= 2000)
                        {
                            comboBodyGSM.Text = "222";
                            comboLoopGrm.Text = "62";
                            comboBoxbottomgsm.Text = "240";
                        }
                    }
                }

            }
            else if (_BodyIndex1 == 1 || _BodyIndex1 == 12 || _BodyIndex1 == 13)
            {
                if (comboSF.SelectedIndex == 0) // 5:1
                {
                    if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        comboBodyGSM.Text = "142";
                    else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                        comboBodyGSM.Text = "162";
                    else if (Utility.SafeConvertToDouble(textSWL.Text) <= 2000)
                        comboBodyGSM.Text = "202";
                }
                else if (comboSF.SelectedIndex == 1) // 6:1
                {
                    if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        comboBodyGSM.Text = "162";
                    else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                        comboBodyGSM.Text = "182";
                    else if (Utility.SafeConvertToDouble(textSWL.Text) <= 2000)
                        comboBodyGSM.Text = "222";
                }
            }
            else if (_BodyIndex1 == 5 || _BodyIndex1 == 7) //Single Loop
            {
                if (comboSF.SelectedIndex == 0) // 5:1
                {
                    if (Utility.SafeConvertToDouble(textSWL.Text) <= 600)
                    {
                        comboBodyGSM.Text = "92";
                        comboBoxbottomgsm.Text = "122";
                    }
                    if (Utility.SafeConvertToDouble(textSWL.Text) <= 1000)
                        comboBodyGSM.Text = "142";
                    else if (Utility.SafeConvertToDouble(textSWL.Text) <= 1500)
                        comboBodyGSM.Text = "162";
                    else if (Utility.SafeConvertToDouble(textSWL.Text) <= 2000)
                        comboBodyGSM.Text = "202";
                }
            }
            else if (_BodyIndex1 == 6 || _BodyIndex1 == 8) //Double Loop
            {
                if (comboSF.SelectedIndex == 0) // 5:1
                {
                    if (Utility.SafeConvertToDouble(textSWL.Text) <= 2000)
                    {
                        comboBodyGSM.Text = "142";
                        comboBoxbottomgsm.Text = "160";
                    }
                }
                if (comboSF.SelectedIndex == 1) // 6:1
                {
                    if (Utility.SafeConvertToDouble(textSWL.Text) <= 2000)
                    {
                        comboBodyGSM.Text = "162";
                        comboBoxbottomgsm.Text = "180";

                    }
                }
            }

            if (_BodyIndex1 == 0) //Upanel
                comboSideGSM.Text = comboBodyGSM.Text;


        }

        private void comboLoopConst_SelectedIndexChanged(object sender, EventArgs e)
        {
            textShortLeg.ReadOnly = false;
            textShortLeg.Text = "50";


            comboLoopW.Text = "5";
            if (comboLoopConst.SelectedIndex == 2) // Cross Corner
            {
                comboLoopW.Text = "7";
                comboLoopL.Text = "30";
                textShortLeg.Text = "0";
                textShortLeg.ReadOnly = true;
            }


            if (comboBody2.SelectedIndex == 4) // Builder Bag
                comboLoopW.Text = "4";
            if (checkBoxTunnel.Checked)
                comboLoopW.Text = "4";

            if (comboBody2.SelectedIndex == 4 || comboBody2.SelectedIndex == 5) //Tunnel and Builder Bag
            {
                if (comboLoopConst.SelectedIndex == 1)
                {
                    comboLoopGrm.Text = "25";
                    comboLoopL.Text = "30";
                    comboLoopW.Text = "4";
                }
            }
            if (comboLoopConst.SelectedIndex == 2 || comboLoopConst.SelectedIndex == 3) // Cross Corner
            {
                chkFabricPatch.Visible = true;
                chkfabricp.Visible = true;
                cmbfabricPatchLamGSM.Visible = true;
            }
            else
            {
                chkFabricPatch.Visible = false;
                chkFabricPatch.Checked = false;
                chkfabricp.Visible = false;
                cmbfabricPatchLamGSM.Visible = false;
            }
        }

        private void comboBody2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBody2.SelectedIndex == 5 || comboBody2.SelectedIndex == 4) //Tunnel,Builder
            {
                if (comboBody2.SelectedIndex == 5)
                    checkBoxTunnel.Checked = true;
                comboBodyGSM.Text = "132";
                comboSideGSM.Text = "122";
                comboTunnelGSM.Text = "132";
                _Type = 1;
            }
            if (comboBody2.SelectedIndex == 8) //TUNNEL CENTER JOINT
            {
                //if (comboBody2.SelectedIndex == 8)
                //    checkBoxTunnel.Checked = true;
                comboBodyGSM.Text = "100";
                comboSideGSM.Text = "100";
                //comboTunnelGSM.Text = "100";
                _Type = 1;
            }
            if (comboBody2.SelectedIndex == 6) //Ventilated
            {
                textBodyRemarks.Text = "Vents";
                textTopRemarks.Text = " Vents";
                textBottomRemarks.Text = "Vents";

            }
            else
            {
                checkBoxTunnel.Checked = false;
                comboBodyGSM.Text = "0";
                comboSideGSM.Text = "0";
                comboTunnelGSM.Text = "0";
                _Type = 0;

            }
            if (_BodyIndex1 == 2) // added by manish on 20th july
            {
                if (comboType.SelectedIndex == 0 && comboBody2.SelectedIndex == 7) // Internal
                {
                    double x = Math.Round(Utility.SafeConvertToDouble(textBodyL.Text) / 3, 1);
                    double y = x + 2;
                    textBodyRemarks.Text = "Buffle Coding " + y.ToString() + "," + x.ToString() + "," + y.ToString();
                }
                else if (comboType.SelectedIndex == 0 && comboBody2.SelectedIndex != 7) // Internal
                {
                    double x = Math.Round(Utility.SafeConvertToDouble(textBodyL.Text) / 3, 1);
                    double y = x + 5.5;
                    textBodyRemarks.Text = "Buffle Coding " + y.ToString() + "," + x.ToString() + "," + y.ToString();

                }
                else
                    textBodyRemarks.Text = "";
            }
            else
                textBodyRemarks.Text = "";

        }
        private void comboBoxbottomdia_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_BodyIndex1 == 0 && comboBoxbottomtype.SelectedIndex == 3)
                textBodyRemarks.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
            else if (_BodyIndex1 == 0 && comboBoxbottomtype.SelectedIndex == 1)
                textBodyRemarks.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);

            else
            {
                if (comboBoxbottomsubtype.SelectedIndex == 1) //Petal Closure
                {
                    if (_BodyIndex1 == 3 || _BodyIndex1 == 1)
                        textBottomrem.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                    // else
                    //   textBottomRemarks.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                }

                else if (comboBoxbottomtype.SelectedIndex == 3) //Bottom Spout
                {
                    textBottomrem.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                }
            }
        }

        private void btnPrintPO_Click(object sender, EventArgs e)
        {
            frmPrintBillOfMaterial frm = new frmPrintBillOfMaterial(true);
            frm.ShowDialog();
        }



        private void textFilePONo_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (Database.OpenConnection(Utility.ProductionConnectionString))
                {
                    IsupdateMode = false;
                    string FileName = "";
                    string fsHt = string.Empty;
                    string fsDia = string.Empty;
                    string LoopLength = string.Empty;
                    string DuffleHT = string.Empty;
                    #region BOM1


                    Database.myreader = Database.GetExecuteReaderCommand("select toptypes,bottomtypes from BOM3 WITH(nolock) where PONo ='" +
                             textFilePONo.Text + "'");
                    if (Database.myreader.Read())
                    {
                        IsupdateMode = true;
                        comboTopType.Text = Database.myreader["Toptypes"].ToString();
                        comboBoxbottomtype.Text = Database.myreader["Bottomtypes"].ToString();
                    }
                    Database.myreader.Close();

                    Database.myreader = Database.GetExecuteReaderCommand("select SysDate,Customer,PrintType, BagType,SizeL,SizeW,SizeH,SizeType,SWL,Doc,ShortLen,LoopL,LoopW,LoopDim,S,L, " +
                        "IsDropLoop, DropLoop,FSType,LinerL,LinerW,LinerDim,Liner,LinerType,Qty,QtyUnit,DSL,DSW,DSDim,FabColor,DSType,FSL,FSW,  ThreadTotalKg," +
                        "FeltWt,TotalKg,Instruction,BodyRemarks1,SrNo , SlitHt,FillHt,TotalHt,BodyGSM,BodyLami,SideGSM,SideLami,DuffleHt,pono,bodyno,printingremarks,refno," +
                        " conicaltop,DOCL,DOCW,DSL1,DSW1,DSL2,DSW2,DSType1,DSType2,looptype,DocNumber,Bottomno,sideno,doc1,doc2,docl1,docw1,docl2,docw2,loopconst,BottomSkritH" +
                      " ,Approvalfield,looplongleg,Knottype,RPfabric,slitlenght from BOM1 WITH(nolock) where FilePONo ='" +
                               textFilePONo.Text + "' and SrNo != 'temp'");
                    if (Database.myreader.Read())
                    {
                        textSlitLength.Text = Database.myreader["slitlenght"].ToString();
                        if (Database.myreader["RPfabric"].ToString() == "30% RP Fabric")
                            checkRPFabric.Checked = true;
                        else
                            checkRPFabric.Checked = false;
                        comboBottomKnotType.Text = Database.myreader["Knottype"].ToString();

                        textLongLeg.Text = Database.myreader["looplongleg"].ToString();

                        if (Database.myreader["Approvalfield"].ToString() != "")
                        {

                            string[] st = Database.myreader["Approvalfield"].ToString().Split(new string[] { Environment.NewLine },
                                                  StringSplitOptions.None);

                            for (int i = 0; i < dgapprovallist.Rows.Count - 1; i++)
                            {
                                for (int j = 0; j < st.Length; j++) 
                                {
                                    if (dgapprovallist.Rows[i].Cells[1].Value.ToString() == st[j])
                                    {
                                        dgapprovallist.Rows[i].Cells[0].Value = "True";
                                    }
                                }
                            }
                        }

                        

                        comboLoopConst.Text = Database.myreader["loopconst"].ToString();
                        textSideNo.Text = Database.myreader["sideno"].ToString();
                        textBottomNo.Text = Database.myreader["Bottomno"].ToString();
                        textDocL.Text = Database.myreader["DOCL"].ToString();
                        textDocW.Text = Database.myreader["DOCW"].ToString();
                        textDoc1L.Text = Database.myreader["DOCL1"].ToString();
                        textDoc1W.Text = Database.myreader["DOCW1"].ToString();
                        textDoc2L.Text = Database.myreader["DOCL2"].ToString();
                        textDoc2W.Text = Database.myreader["DOCW2"].ToString();

                        IsupdateMode = true;
                        textprintingremarks.Text = Database.myreader["printingremarks"].ToString();
                        EnqdateTime.Text = Database.myreader["SysDate"].ToString();
                        comboPartyName.Text = Database.myreader["Customer"].ToString();
                        comboPrintType.Text = Database.myreader["PrintType"].ToString();
                        textpono.Text = Database.myreader["pono"].ToString();
                        textSlitHt.Text = Database.myreader["slitht"].ToString();
                        textFillHt.Text = Database.myreader["FillHt"].ToString();
                        comboLoopType.Text = Database.myreader["looptype"].ToString();

                        if (Database.myreader["BagType"].ToString().Contains("Tunnel"))
                        {
                            checkBoxTunnel.Checked = true;
                            string[] str = Database.myreader["BagType"].ToString().Split('/');
                            comboBody1.Text = str[0];
                            if (str.Length == 2)
                            {
                                comboBody2.Text = str[1];
                            }
                            if (str.Length > 2)
                            {
                                comboBagType.Text = str[2];
                            }
                        }
                        else
                        {
                            string[] str = Database.myreader["BagType"].ToString().Split('/');
                            comboBody1.Text = str[0];
                            comboBody2.Text = str[1];
                            comboBody3.Text = str[2];
                            comboBagType.Text = "";
                            if (str.Length > 3)
                            {
                                comboBagType.Text = str[3];
                            }

                        }

                        textBodyL.Text = Database.myreader["SizeL"].ToString();
                        textBodyW.Text = Database.myreader["SizeW"].ToString();
                        textBodyH.Text = Database.myreader["SizeH"].ToString();
                        comboType.Text = Database.myreader["SizeType"].ToString();
                        textSWL.Text = Database.myreader["SWL"].ToString();

                        if (Database.myreader["Doc"].ToString() != "N/A")
                        {
                            checkBoxdocpouch.Checked = true;
                            string[] str = Database.myreader["Doc"].ToString().Split('/');
                            comboDocType.Text = str[0];
                            comboDocType1.Text = str[1];
                            comboDocType2.Text = str[2];
                        }

                        if (Database.myreader["Doc1"].ToString() != "N/A")
                        {
                            if (Database.myreader["Doc1"].ToString() != "")
                            {
                                //MessageBox.Show(Database.myreader["Doc1"].ToString());
                                checkdocpouch1.Checked = true;
                                string[] str = Database.myreader["Doc1"].ToString().Split('/');
                                combodoctype3.Text = str[0];
                                combodoctype4.Text = str[1];
                                combodoctype5.Text = str[2];
                            }
                        }


                        if (Database.myreader["Doc2"].ToString() != "N/A")
                        {
                            if (Database.myreader["Doc2"].ToString() != "")
                            {
                                checkdocpouch2.Checked = true;
                                string[] str = Database.myreader["Doc2"].ToString().Split('/');
                                combodoctype6.Text = str[0];
                                combodoctype7.Text = str[1];
                                combodoctype8.Text = str[2];
                            }
                        }

                        checkBoxDropLoop.Checked = false;
                        if (Database.myreader["IsDropLoop"].ToString() == "yes")
                        {
                            checkBoxDropLoop.Checked = true;
                            textLoopDropLenght.Text = Database.myreader["DropLoop"].ToString();
                        }

                        if (Utility.SafeConvertToDouble(Database.myreader["ShortLen"].ToString()) > 0)
                        {
                            textShortLeg.Text = Database.myreader["ShortLen"].ToString().Trim();
                            LoopLength = Database.myreader["LoopL"].ToString().Trim();
                            comboLoopW.Text = Database.myreader["LoopW"].ToString().Trim();
                        }
                        LoopLength = Database.myreader["LoopL"].ToString().Trim();

                        comboBodyUnit.Text = Database.myreader["LoopDim"].ToString();
                        comboSF.Text = Database.myreader["S"].ToString();

                        DuffleHT = Database.myreader["DuffleHt"].ToString();
                        //  moved to bom3 TopType,duffleskirtheight,
                        if (Database.myreader[17].ToString() != "Open") //change [17] to 18 for fstype 21.09.2021
                        {
                            checkBoxTop.Checked = true;
                            if (Database.myreader["FSType"].ToString().Contains("Skrit"))
                            {
                                // comboTopType.Text = Database.myreader[17].ToString();
                                //  comboBoxduffleskirtheight.Text = Database.myreader[143].ToString();
                            }
                            else
                            {
                                //comboTopType.Text = Database.myreader[16].ToString();
                                comboSpoutType.Text = Database.myreader["FSType"].ToString();
                            }
                        }

                        if (Database.myreader["LinerType"].ToString() != "N/A")
                        {
                            checkBoxliner.Checked = true;
                            comboBoxlinerheight.Text = Database.myreader["LinerL"].ToString();
                            comboBoxlinerwidth.Text = Database.myreader["LinerW"].ToString();
                            comboBoxlinermicron.Text = Database.myreader["LinerDim"].ToString();
                            comboBoxlinertype.Text = Database.myreader["Liner"].ToString();
                            if (Database.myreader["LinerType"].ToString().Contains("|"))
                            {
                                comboBoxlinertype1.Items.Add(Database.myreader["LinerType"].ToString().Split('|')[0].ToString());
                                comboBoxlinertype1.Text = Database.myreader["LinerType"].ToString().Split('|')[0].ToString();
                            }
                            else
                            {
                                 comboBoxlinertype1.Items.Add( Database.myreader["LinerType"].ToString());
                                comboBoxlinertype1.Text = Database.myreader["LinerType"].ToString();
                            }
                        }
                        textQty.Text = Database.myreader["Qty"].ToString();

                        if (Utility.SafeConvertToDouble(Database.myreader["DSL"].ToString()) > 0)
                        {
                            checkbottom.Checked = true;
                            string[] str = Database.myreader["DSType"].ToString().Split('/');
                            comboBoxbottomtype.Text = str[0];
                            comboBoxbottomsubtype.Text = str[1];
                            if (str.Length == 3)
                                comboBoxbottomsubtype.Text = str[1] + "/" + str[2];
                            comboBoxbottomdia.Text = Database.myreader["DSL"].ToString();
                            comboBoxbottomheight.Text = Database.myreader["DSW"].ToString();
                        }

                        if (Utility.SafeConvertToDouble(Database.myreader["DSL1"].ToString()) > 0)
                        {
                            checkBottom1.Checked = true;
                            string[] str = Database.myreader["DSType1"].ToString().Split('/');
                            comboBoxbottomtype1.Text = str[0];
                            comboBoxbottomsubtype1.Text = str[1];
                            if (str.Length == 3)
                                comboBoxbottomsubtype1.Text = str[1] + "/" + str[2];
                            comboBoxbottomdia1.Text = Database.myreader["DSL1"].ToString();
                            comboBoxbottomheight1.Text = Database.myreader["DSW1"].ToString();
                        }

                        if (Utility.SafeConvertToDouble(Database.myreader["DSL2"].ToString()) > 0)
                        {
                            checkBottom2.Checked = true;
                            string[] str = Database.myreader["DSType2"].ToString().Split('/');
                            comboBoxbottomtype2.Text = str[0];
                            comboBoxbottomsubtype2.Text = str[1];
                            if (str.Length == 3)
                                comboBoxbottomsubtype2.Text = str[1] + "/" + str[2];
                            comboBoxbottomdia2.Text = Database.myreader["DSL2"].ToString();
                            comboBoxbottomheight2.Text = Database.myreader["DSW2"].ToString();
                        }


                        comboBodyColor.Text = Database.myreader["FabColor"].ToString();
                        if (Utility.SafeConvertToDouble(Database.myreader["FSL"].ToString()) > 0)
                        {
                            fsDia = Database.myreader["FSL"].ToString();
                            fsHt = Database.myreader["FSW"].ToString();
                            //comboSpoutDia.Text = Database.myreader["FSL"].ToString();
                            //comboSpoutHeight.Text = Database.myreader["FSW"].ToString();
                        }

                        comboBodyGSM.Text = Database.myreader["BodyGSM"].ToString();

                        comboBodyLamiGSM.Text = Database.myreader["BodyLami"].ToString();
                        if (Utility.SafeConvertToDouble(Database.myreader["BodyLami"].ToString()) > 0)
                        {
                            checkBoxLam.Checked = true;
                            comboBodyLamiGSM.Text = Database.myreader["BodyLami"].ToString();
                        }
                        #region comment
                        //else
                        //    comboBodyGSM.Text = Database.myreader[33].ToString();

                        //if (Utility.SafeConvertToDouble(Database.myreader["SideGSM"].ToString()) > 0)
                        //{
                        //    checkSide.Checked = true;
                        //    groupSide.Visible = true;
                        //    comboSideGSM.Text = Database.myreader["SideGSM"].ToString();
                        //    if (Utility.SafeConvertToDouble(Database.myreader["SideLami"].ToString()) > 0)
                        //    {
                        //        checkSideLami.Checked = true;
                        //        comboSideLamiGSM.Visible = true;
                        //        comboSideLamiGSM.Text = Database.myreader["SideLami"].ToString();
                        //    }

                        //}

                        //if (Database.myreader[47].ToString() != "")
                        //{
                        //    checkBoxTop.Checked = true;
                        //    if (Database.myreader[47].ToString().Contains("+"))
                        //    {
                        //        checkBoxTopLam.Checked = true;
                        //        comboSpoutLamiGSM.Visible = true;
                        //        string[] str = Database.myreader[47].ToString().Split('+');
                        //        comboBoxTopGSM.Text = str[0];
                        //        comboTopLamiGSM.Text = str[1].Trim();
                        //    }
                        //    else
                        //        comboBoxTopGSM.Text = Database.myreader[47].ToString();
                        //    comboTopColor.Text = Database.myreader[53].ToString();
                        //}

                        //if (Database.myreader[54].ToString() != "")
                        //{
                        //    checkbottom.Checked = true;
                        //    if (Database.myreader[54].ToString().Contains("+"))
                        //    {
                        //        checkBoxbottomlam.Checked = true;
                        //        string[] str = Database.myreader[54].ToString().Split('+');
                        //        comboBoxbottomgsm.Text = str[0];
                        //        comboBottomLamiGSM.Text = str[1].Trim();
                        //    }
                        //    else
                        //        comboBoxbottomgsm.Text = Database.myreader[54].ToString();

                        //    comboBottomColor.Text = Database.myreader[60].ToString();
                        //}


                        //if (Database.myreader[61].ToString() != "")
                        //{
                        //    if (Database.myreader[61].ToString().Contains("+"))
                        //    {
                        //        checkBoxSpoutLam.Checked = true;
                        //        string[] str = Database.myreader[61].ToString().Split('+');
                        //        comboSpoutGSM.Text = str[0];
                        //        comboSpoutLamiGSM.Text = str[1].Trim();
                        //    }
                        //    else
                        //        comboSpoutGSM.Text = Database.myreader[61].ToString();
                        //    comboSpoutColor.Text = Database.myreader[67].ToString();
                        //}


                        //if (Database.myreader[68].ToString() != "")
                        //{
                        //    checkSpoutTie.Checked = true;
                        //    comboSpoutTieGrm.Text = Database.myreader[68].ToString();
                        //    comboTopSpoutTieColor.Text = Database.myreader[74].ToString();
                        //}

                        //if (Database.myreader[75].ToString() != "")
                        //{
                        //    if (Database.myreader[75].ToString().Contains("+"))
                        //    {
                        //        string[] str = Database.myreader[75].ToString().Split('+');
                        //        comboBoxbottomgsm1.Text = str[0];
                        //        comboBoxBottomSubTypeLamiGSM.Text = str[1];
                        //    }
                        //    else
                        //        comboBoxbottomgsm1.Text = Database.myreader[75].ToString();
                        //    comboBottomSpoutColor.Text = Database.myreader[81].ToString();
                        //}

                        //if (Database.myreader[82].ToString() != "")
                        //{
                        //    checkBottomSpoutTie.Checked = true;
                        //    comboBottomSpoutTieGrm.Text = Database.myreader[82].ToString();
                        //    comboBottomSpoutTieColor.Text = Database.myreader[88].ToString();
                        //}

                        //if (Database.myreader[89].ToString() != "")
                        //{
                        //    checkBoxLoop.Checked = true;
                        //    comboLoopL.Text = Database.myreader[12].ToString();
                        //    comboLoopW.Text = Database.myreader[13].ToString();
                        //    comboLoopGrm.Text = Database.myreader[89].ToString();
                        //    comboLoopColor.Text = Database.myreader[90].ToString();
                        //    textLoopNo.Text = Database.myreader[95].ToString();
                        //    textLoopLenght.Text = Database.myreader["LoopCutSize"].ToString();
                        //}

                        //if (Database.myreader[102].ToString() != "")
                        //    comboLinerColor.Text = Database.myreader[102].ToString();

                        //if (Database.myreader[103].ToString() != "")
                        //{
                        //    checkBoxdocpouch.Checked = true;
                        //    comboDocMicron.Text = Database.myreader[103].ToString();
                        //    comboDocColor.Text = Database.myreader[108].ToString();
                        //}

                        //if (Database.myreader[109].ToString() != "")
                        //{
                        //    comboLabelMicron.Text = Database.myreader[109].ToString();
                        //    comboLabelColor.Text = Database.myreader[114].ToString();
                        //}
                        //textInstruction.Text = Database.myreader[121].ToString();
                        //textBodyRemarks.Text = Database.myreader[122].ToString();

                        //BOMNo = Database.myreader[123].ToString();
                        //if (textSlitHt.Text != "")
                        //{
                        //    textSlitHt.Text = Database.myreader[124].ToString();
                        //    textFillHt.Text = Database.myreader[125].ToString();
                        //}

                        //textBottomRemarks.Text = Database.myreader[127].ToString();
                        //textTopSpoutTieRemarks.Text = Database.myreader[128].ToString();
                        //textBottomSpoutTieRemarks.Text = Database.myreader[129].ToString();

                        //if (Database.myreader["IsDropLoop"].ToString() == "yes")
                        //{
                        //    checkBoxDropLoop.Checked = true;
                        //    textLoopDropLenght.Text = Database.myreader["DropLoop"].ToString();
                        //}


                        ////textFile1.Text = Database.myreader[138].ToString();
                        ////FileName = Database.myreader[139].ToString();

                        //textRMPP.Text = Database.myreader[132].ToString();
                        //textStdConvPP.Text = Database.myreader[133].ToString();
                        //textRMPE.Text = Database.myreader[134].ToString();
                        //textStdConvPE.Text = Database.myreader[135].ToString();
                        //textDoc.Text = Database.myreader[136].ToString();
                        //textPallets.Text = Database.myreader[137].ToString();
                        //textPrintingRate.Text = Database.myreader[138].ToString();
                        //textBLock.Text = Database.myreader[139].ToString();
                        //textHoseSlider.Text = Database.myreader[140].ToString();
                        //textVelcro.Text = Database.myreader[141].ToString();
                        //textDustProof.Text = Database.myreader[142].ToString();
                        //textFelt.Text = Database.myreader[143].ToString();
                        //textFrieght.Text = Database.myreader[144].ToString();

                        //textFile1.Text = Database.myreader[155].ToString();
                        //FileName = Database.myreader[156].ToString();

                        //comboBoxduffleskirtheight.Text = Database.myreader[157].ToString();

                        //comboCurrency.Text = Database.myreader[158].ToString();
                        //textDiscount.Text = Database.myreader[159].ToString();
                        //if (Database.myreader[160].ToString() == "Confirmed")
                        //    checkOrderConfirmed.Checked = true;
                        //textDocNo.Text = Database.myreader["DocNumber"].ToString();
                        textSkirtHeight.Text = Database.myreader["BottomSkritH"].ToString(); //20.09.2021

                        //btnSave.Enabled = false;
                        #endregion
                        btnUpdate.Enabled = true;
                        //  textFilePONo.Enabled = false;
                        FilePONo = textFilePONo.Text;
                        textBodyNo.Text = Database.myreader["BodyNo"].ToString();
                        textInstruction.Text = Database.myreader["Instruction"].ToString();
                        textRefNo.Text = Database.myreader["RefNo"].ToString();
                        textConicaltop.Text = Database.myreader["conicaltop"].ToString();

                        textDocNo.Text = Database.myreader["DocNumber"].ToString();

                    }
                    Database.myreader.Close();

                    #endregion
                    #region bom
                    bool isBodylami = false;

                    DataTable DtFillBOM = new DataTable();
                    SqlDataAdapter myadpter1 = Database.GetAdapterCommand("Select   dbo.BOM.Heading  ,dbo.BOM.GSM , dbo.BOM.Lami , dbo.BOM.Color , " +
                    " dbo.BOM.FabricSize , dbo.BOM.CutSize , dbo.BOM.TotalMtr , dbo.BOM.TotalKg as HeadTotalKG ,BOM.Remarks,dbo.BOM.PONo,dbo.BOM.SrNo,dbo.BOM.gpm  from BOM WITH(nolock) where dbo.BOM.PONo='" + textFilePONo.Text + "'");

                    myadpter1.Fill(DtFillBOM);

                    int index = 0;
                    dataGridView1.Rows.Clear();
                    for (int i = 0; i < DtFillBOM.Rows.Count; i++)
                    {
                        if (DtFillBOM.Rows[i]["Heading"].ToString().Contains(" ~")) //other components which is directly taken from datagridview
                        {
                            dataGridView1.Rows.Add();
                            dataGridView1.Rows[index].Cells[0].Value = DtFillBOM.Rows[i]["Heading"].ToString().Remove(DtFillBOM.Rows[i]["Heading"].ToString().Length - 1).ToString();
                            dataGridView1.Rows[index].Cells[1].Value = DtFillBOM.Rows[i][1].ToString();
                            dataGridView1.Rows[index].Cells[2].Value = DtFillBOM.Rows[i][2].ToString();
                            dataGridView1.Rows[index].Cells[3].Value = DtFillBOM.Rows[i][3].ToString();
                            dataGridView1.Rows[index].Cells[4].Value = DtFillBOM.Rows[i][4].ToString();
                            dataGridView1.Rows[index].Cells[5].Value = DtFillBOM.Rows[i][5].ToString();
                            dataGridView1.Rows[index].Cells[6].Value = DtFillBOM.Rows[i][6].ToString();
                            dataGridView1.Rows[index].Cells[7].Value = DtFillBOM.Rows[i][7].ToString();
                            dataGridView1.Rows[index].Cells[8].Value = DtFillBOM.Rows[i][8].ToString();
                            dataGridView1.Rows[index].Cells[9].Value = DtFillBOM.Rows[i][11].ToString();
                            index++;
                        }
                        if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Petal Flap") // Top Petal Flap
                        {
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkPetalFlapGSMLam.Checked = true;
                                string[] s1 = DtFillBOM.Rows[i]["GSM"].ToString().Split('+');

                                comboTopPetalFlapGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                                comboTopPetalFlapGSMLam.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                            }
                            else
                                comboTopPetalFlapGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                           
                        }


                        if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Petal Flap") // Top Petal Flap
                        {
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkbottomPetalFlapGSM.Checked = true;
                                string[] s1 = DtFillBOM.Rows[i]["GSM"].ToString().Split('+');

                                comboBottomPetalFlapGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                                combobottomPetalFlapGSMLam.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                            }
                            else
                                comboBottomPetalFlapGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                           
                        }

                        if (DtFillBOM.Rows[i]["Heading"].ToString() == "Body") // Body read
                        {
                            checkBoxLam.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkBoxLam.Checked = true;
                                isBodylami = true;
                                string[] s1 = DtFillBOM.Rows[i]["GSM"].ToString().Split('+');

                                if (s1.Length == 3)
                                    comboBodyLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim() + "+" + DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[2].Trim();
                                else
                                    comboBodyLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboBodyGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboBodyGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();

                            comboBodyColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();

                            textBodyRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Side") // Side read                       
                        {
                            checkSide.Checked = true;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkSideLami.Checked = true;
                                string[] s1 = DtFillBOM.Rows[i]["GSM"].ToString().Split('+');

                                if (s1.Length == 3)
                                    comboSideLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim() + "+" + DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[2].Trim();
                                else
                                    comboSideLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboSideGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboSideGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboSideColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();

                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top")  // Top read
                        {

                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkBoxTopLam.Checked = true;
                                if (!comboBoxTopGSM.Text.Contains(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim()))
                                    comboBoxTopGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim());


                                if (!comboTopLamiGSM.Text.Contains(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim()))
                                    comboTopLamiGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim());

                                comboTopLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboBoxTopGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();

                            }
                            else
                                comboBoxTopGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();

                            comboTopColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            textTopRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();

                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "FS Hose Slider")  // Top read
                        {
                            comboTopHoseSlider.Text = DtFillBOM.Rows[i]["FabricSize"].ToString();
                            comboTopHoseSliderCutSize.Text = DtFillBOM.Rows[i]["CutSize"].ToString();
                            textHoseSliderNo.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Split(' ')[1].Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "DS Hose Slider")  // Top read
                        {
                            comboBottomhoseslider.Text = DtFillBOM.Rows[i]["FabricSize"].ToString();
                            comboBottomhosesliderCutsize.Text = DtFillBOM.Rows[i]["CutSize"].ToString();
                            textBottomhosesliderno.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Split(' ')[1].Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "FabricPatch")  // Top read
                        {
                            chkFabricPatch.Checked = true;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                chkfabricp.Checked = true;
                                if (!cmbfabricpatchGSM.Text.Contains(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim()))
                                    cmbfabricpatchGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim());


                                if (!cmbfabricPatchLamGSM.Text.Contains(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim()))
                                    cmbfabricPatchLamGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim());


                                cmbfabricPatchLamGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                cmbfabricpatchGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();

                            }
                            else
                                cmbfabricpatchGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();

                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom")  // Bottom read
                        {
                            checkbottom.Checked = true;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkBoxbottomlam.Checked = true;
                                if (!comboBoxbottomgsm.Text.Contains(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim()))
                                    comboBoxbottomgsm.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim());

                                if (!comboBottomLamiGSM.Text.Contains(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim()))
                                    comboBottomLamiGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim());

                                comboBottomLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                               
                                comboBoxbottomgsm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboBoxbottomgsm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboBottomColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();

                            textBottomrem.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();

                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Spout")  // Top Spout/ FS Spout read
                        {
                            comboTopType.SelectedIndex = 1;
                            checkBoxSpoutLam.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                groupSpout.Visible = true;
                                groupBox6.Visible = true;
                                checkBoxSpoutLam.Checked = true;
                                if (!comboSpoutGSM.Text.Contains(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim()))
                                    comboSpoutGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim());

                                if (!comboSpoutLamiGSM.Text.Contains(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim()))
                                    comboSpoutLamiGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim());


                                comboSpoutLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboSpoutGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboSpoutGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboSpoutColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();

                            textTopSpoutRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Spout Tie"
                            )  //  FSTie || DtFillBOM.Rows[i]["Heading"].ToString() == "IRIS Tie"
                        {
                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Spout Tie")
                                checkSpoutTie.Checked = true;
                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "IRIS Tie")
                                checkIRISTie.Checked = true;


                            comboTopSpoutTieColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            comboSpoutTieGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();

                            textTopSpoutTieRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "IRIS Tie")  //  FSTie 29.09.2021
                        {

                            checkIRISTie.Checked = true;
                            comboTopSpoutTieIRISColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            comboSpoutTieIRISGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            textTopSpoutTieIRISRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout")  // Bottom Spout
                        {
                            checkBoxbottomlam1.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkBoxbottomlam1.Checked = true;
                                if (!comboBoxbottomgsm1.Text.Contains(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim()))
                                    comboBoxbottomgsm1.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim());

                                if (!comboBoxBottomSubTypeLamiGSM.Text.Contains(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim()))
                                    comboBoxBottomSubTypeLamiGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim());

                                comboBoxBottomSubTypeLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                              
                                comboBoxbottomgsm1.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboBoxbottomgsm1.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboBottomSpoutColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();

                            textBottomRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();

                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout1")  // Bottom Spout
                        {
                            checkBoxbottomlam3.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkBoxbottomlam3.Checked = true;
                                comboBoxBottomSubTypeLamiGSM1.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboBoxbottomgsm3.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboBoxbottomgsm3.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboBottomSpoutColor1.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();

                            textBottomRemarks1.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();

                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout2")  // Bottom Spout
                        {
                            checkBoxbottomlam5.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkBoxbottomlam5.Checked = true;
                                comboBoxBottomSubTypeLamiGSM2.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboBoxbottomgsm5.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboBoxbottomgsm5.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboBottomSpoutColor2.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();

                            textBottomRemarks2.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout Tie")  // Bottom Spout 29.09.2021
                        {
                            checkBottomSpoutTie.Checked = true;
                            comboBottomSpoutTieColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                                comboBottomSpoutTieGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else
                                comboBottomSpoutTieGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();

                            textBottomSpoutTieRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "IRIS Bottom Tie")  // Bottom Spout 29.09.2021
                        {
                            checkBottomspoutiristie.Checked = true;
                            comboBottomSpoutTieIRISColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                                comboBottomSpoutTieIRISGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else
                                comboBottomSpoutTieIRISGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();

                            textBottomSpoutTieIRISRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout Tie1")  // Bottom Spout
                        {
                            checkBottomSpoutTie1.Checked = true;
                            comboBottomSpoutTieColor1.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                                comboBottomSpoutTieGrm1.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else
                                comboBottomSpoutTieGrm1.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            textBottomSpoutTieRemarks1.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout Tie2")  // Bottom Spout
                        {
                            checkBottomSpoutTie2.Checked = true;
                            comboBottomSpoutTieColor2.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                                comboBottomSpoutTieGrm2.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else
                                comboBottomSpoutTieGrm2.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            textBottomSpoutTieRemarks2.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Loop")  // Loop Spout
                        {
                            checkBoxLoop.Checked = true;
                            comboLoopColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            if (isBodylami)
                                comboLoopGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else
                                comboLoopGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            textLoopRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Loop")  // Bottom Loop
                        {
                            checkBottomloop.Checked = true;
                            comboBottomLoopColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            comboBottomLoopgrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            textBottomLoopLenght.Text = DtFillBOM.Rows[i]["CutSize"].ToString().Trim();
                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Liner")  // Liner Spout
                        {
                            checkBoxliner.Checked = true;
                            comboLinerColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                                comboBoxlinermicron.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else comboBoxlinermicron.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            //combolinersubtype.Text = textLinerRemarks.Text;
                            textLinerRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "DocPouch")  // DocPouch Spout
                        {
                            checkBoxdocpouch.Checked = true;
                            comboDocColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                                comboDocMicron.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else
                                comboDocMicron.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            textDocRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();

                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "DocPouch1")  // DocPouch Spout
                        {
                            checkdocpouch1.Checked = true;
                            comboDoc1Color.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                                comboDoc1Micron.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else
                                comboDoc1Micron.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();

                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "DocPouch2")  // DocPouch Spout
                        {
                            checkdocpouch2.Checked = true;
                            comboDoc2Color.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                                comboDoc2Micron.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else
                                comboDoc2Micron.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();

                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Label")  // DocPouch Spout
                        {
                            checkBoxlabel.Checked = true;
                            comboLabelColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                                comboLabelMicron.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else
                                comboLabelMicron.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            textLabelRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Reinforce fabric" || DtFillBOM.Rows[i]["Heading"].ToString() == "Tunnel")  // Top Spout/ FS Spout read
                        {
                            comboTunnelDesign.SelectedIndex = 0;
                            checkBoxTunnel.Checked = true;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkTunnelLam.Checked = true;
                                comboTunnelLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboTunnelGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboTunnelGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                        }


                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Buffle" ||
                            DtFillBOM.Rows[i]["Heading"].ToString() == "Net Buffle")  //Buffle read
                        {
                            //comboBody1.Text=""
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                            {
                                textSingleCoatedGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                textDoubleCoatedGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[2].Trim();
                                textBuffleGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                textBuffleGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            textBufflecutlenght.Text = DtFillBOM.Rows[i]["CutSize"].ToString().Trim();
                            textBuffleRemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();


                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Ancillay Loop")  //Buffle read
                        {
                            checkAncerieLoop.Checked = true;
                            comboAncillaryLooptype.Text = DtFillBOM.Rows[i]["Lami"].ToString();
                            comboAncerieWidth.Text = DtFillBOM.Rows[i]["fabricsize"].ToString();
                            if (DtFillBOM.Rows[i]["GSM"].ToString().Contains("+"))
                                comboAncerieGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            else comboAncerieGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Filler Cord")  //Buffle read
                        {
                            checkFillerCord.Checked = true;

                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Felt")  //Buffle read
                        {
                            checkFelt.Checked = true;

                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Felt-Under Loop")  //Buffle read
                        {
                            checkFeltUnderTheLoop.Checked = true;

                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Flap")  //Top Flap read
                        {
                            checktopflap.Checked = true;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkTopFlapLami.Checked = true;
                                comboTopflapLamiGsm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboBoxtopflapgsm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboBoxtopflapgsm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();

                            textTopFlapCutlenght.Text = DtFillBOM.Rows[i]["cutsize"].ToString().Trim();
                            textTopFlapFabricsize.Text = DtFillBOM.Rows[i]["fabricsize"].ToString().Trim();


                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Flap")  //Top Flap read
                        {
                            checkBottomflap.Checked = true;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkBottomflap.Checked = true;
                                checkBottomFlapLami.Visible = true;
                                checkBottomFlapLami.Checked = true;
                                comboBottomflapLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboBottomflapGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                                comboBottomflapLamiGSM.Visible = true;
                            }
                            else
                                comboBottomflapGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();

                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Rope")  //Top Rope read
                        {
                            checkTopRope.Checked = true;
                            comboTopRopeGrms.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboTopRopeColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();

                            comboTopRopeSizes.Text = (Utility.SafeConvertToDouble(DtFillBOM.Rows[i]["FabricSize"].ToString().Trim())).ToString();
                            comboTopRopeCutSizes.Text = DtFillBOM.Rows[i]["CutSize"].ToString().Trim();
                            comboTopRopeTypes.Text = DtFillBOM.Rows[i]["Lami"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Rope")  //Top Rope read
                        {
                            checkBottomRope.Checked = true;
                            comboBottomRopeGrms.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboBottomRopeColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            comboBottomRopeSizes.Text = (Utility.SafeConvertToDouble(DtFillBOM.Rows[i]["FabricSize"].ToString().Trim())).ToString();
                            comboBottomRopeCutSizes.Text = DtFillBOM.Rows[i]["CutSize"].ToString().Trim();
                            comboBottomRopeTypes.Text = DtFillBOM.Rows[i]["Lami"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "TopSpout Rope" ||
                            DtFillBOM.Rows[i]["Heading"].ToString() == "IRIS Rope" ||
                            DtFillBOM.Rows[i]["Heading"].ToString() == "Top Petal Rope"
                            )  //Top Rope read
                        {
                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "TopSpout Rope")
                            {
                                checkSpoutRope.Checked = true;
                                checkSpoutRope.Visible = true;
                            }
                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "IRIS Rope")
                                checkIRISRope.Checked = true;

                            comboTopSpoutRopeGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboTopSpoutRopeColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            texttopspoutroperemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Petal Rope" || DtFillBOM.Rows[i]["Heading"].ToString() == "BottomSpout Rope"
                            || DtFillBOM.Rows[i]["Heading"].ToString() == "IRIS Bottom Rope")  //BottomSpout Rope read
                        {
                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "IRIS Bottom Rope")
                                checkBottomspoutirisrope.Checked = true;

                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "Petal Rope")
                            {
                                comboBoxbottomsubtype.SelectedIndex = 1;
                                checkBottomSpoutRope.Checked = true;
                            }
                            //else
                            //    comboBoxbottomsubtype.SelectedIndex = 0;

                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "BottomSpout Rope")
                                checkBottomSpoutRope.Checked = true;

                            //comboBottomSpoutRopeGrm.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Trim());
                            comboBottomSpoutRopeGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboBottomRopeColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();

                            comboBottomSpoutRopeSize.Text = DtFillBOM.Rows[i]["FabricSize"].ToString().Trim();
                            textBottomspoutroperemarks.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();

                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Petal Rope1" || DtFillBOM.Rows[i]["Heading"].ToString() == "BottomSpout Rope1")  //BottomSpout Rope read
                        {
                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "Petal Rope1")
                                comboBoxbottomsubtype1.SelectedIndex = 1;
                            else
                                comboBoxbottomsubtype1.SelectedIndex = 0;

                            checkBottomSpoutRope1.Checked = true;
                            comboBottomSpoutRopeGrm1.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboBottomSpoutRopeColor1.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            textBottomspoutroperemarks1.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Petal Rope2" || DtFillBOM.Rows[i]["Heading"].ToString() == "BottomSpout Rope2")  //BottomSpout Rope read
                        {
                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "Petal Rope2")
                                comboBoxbottomsubtype2.SelectedIndex = 1;
                            else
                                comboBoxbottomsubtype2.SelectedIndex = 0;

                            checkBottomSpoutRope2.Checked = true;
                            comboBottomSpoutRopeGrm2.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboBottomSpoutRopeColor2.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            textBottomspoutroperemarks2.Text = DtFillBOM.Rows[i]["Remarks"].ToString().Trim();
                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Hook")  //Top Hook read
                        {
                            checkTopFlapHook.Checked = true;
                            comboTopflapHookGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboTopHookColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Hook")  //Top Hook read
                        {
                            checkBottomFlapHook.Checked = true;
                            comboBottomFlapHookGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboBottomHookColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Tie")  //Top Hook read
                        {
                            checkTopTie.Checked = true;
                            comboTopTieGrms.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboTopTieColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                            textTopTieNo.Text = GetTieNos(DtFillBOM.Rows[i]["GSM"].ToString().Trim(), DtFillBOM.Rows[i]["Cutsize"].ToString().Trim(),
                                Utility.SafeConvertToDouble(DtFillBOM.Rows[i]["HeadTotalKG"].ToString().Trim())).ToString();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Tie")  //Top Hook read
                        {
                            checkBottomTie.Checked = true;
                            comboBottomTieGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboBottomTieColor.Text = DtFillBOM.Rows[i]["Color"].ToString().Trim();
                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Leno" || DtFillBOM.Rows[i]["Heading"].ToString() == "Jute Skirt"
                            || DtFillBOM.Rows[i]["Heading"].ToString() == "Top Duffle/Skrit")  //Top Hook read
                        {
                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "Leno")
                                comboTopType.SelectedIndex = 6;
                            if (DtFillBOM.Rows[i]["Heading"].ToString() == "Jute Skirt")
                                comboTopType.SelectedIndex = 9;
                            else
                                comboTopType.SelectedIndex = 2;
                            checkBoxTopLam.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkBoxTopLam.Checked = true;
                                comboTopLamiGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim());
                                comboTopLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboBoxTopGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboBoxTopGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString();
                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Duffle/Skrit")  //Top Hook read
                        {
                            comboBoxbottomtype.SelectedIndex = 8;
                            checkBoxbottomlam.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkBoxbottomlam.Checked = true;
                                comboBottomLamiGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim());
                                comboBottomLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboBoxbottomgsm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboBoxbottomgsm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Full Loop")  //Top Hook read
                        {
                            comboLoopConst.SelectedIndex = 3;
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Band")  //Top Band read
                        {
                            checkBoxTopBand.Checked = true;
                            comboTopBandColor.Text = DtFillBOM.Rows[i]["color"].ToString().Trim();
                            combotopbandgrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Belly Band 1")  //18.06.2021
                        {
                            checkBoxTopBellyBand1.Checked = true;
                            comboTopBellyBand1Color.Text = DtFillBOM.Rows[i]["color"].ToString().Trim();
                            combotopBellyband1grm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboTopBellyBand1Size.Text = DtFillBOM.Rows[i]["FabricSize"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Belly Band 2")  //18.06.2021
                        {
                            checkBoxTopBellyBand2.Checked = true;
                            comboTopBellyBand2Color.Text = DtFillBOM.Rows[i]["color"].ToString().Trim();
                            combotopBellyband2grm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboTopBellyBand2Size.Text = DtFillBOM.Rows[i]["FabricSize"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Band")  //18.06.2021
                        {
                            checkBoxTopBottomBand.Checked = true;
                            comboTopBottomBandColor.Text = DtFillBOM.Rows[i]["color"].ToString().Trim();
                            combotopBottomBandgrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboTopBottomBandSize.Text = DtFillBOM.Rows[i]["FabricSize"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Loop Cover")  //Loop Coverread
                        {
                            checkLoopCover.Checked = true;
                            checkLoopCoverLami.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkLoopCoverLami.Checked = true;
                                comboLoopCoverLamiGSM.Items.Add(DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim());

                                comboLoopCoverLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboLoopCoverGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboLoopCoverGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboLoopCoverColor.Text = DtFillBOM.Rows[i]["color"].ToString().Trim();

                            comboLoopCoverCutSize.Items.Add(DtFillBOM.Rows[i]["Cutsize"].ToString().Split('+')[0].Trim());

                            comboLoopCoverCutSize.Text = DtFillBOM.Rows[i]["Cutsize"].ToString().Split('+')[0].Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Stevedore Cover")  //Loop Coverread
                        {
                            checkStevedorecover.Checked = true;
                            checkStevedoreL.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkStevedoreL.Checked = true;
                                comboSteveCoverL.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboSteveCoverGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboSteveCoverGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboStevecoverColor.Text = DtFillBOM.Rows[i]["color"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Inner Box") //21.09.2021 Loop Cover")  //Loop Coverread
                        {
                            checkInnerBox.Checked = true;
                            checkInnerBoxLami.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkInnerBoxLami.Checked = true;
                                comboInnerBoxLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboInnerBoxGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboInnerBoxGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboInnerBoxColor.Text = DtFillBOM.Rows[i]["color"].ToString().Trim();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Loop Protector")  //Loop Coverread
                        {
                            checkLoopProtector.Checked = true;
                            checkLoopProcLami.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkLoopProcLami.Checked = true;
                                comboLoopProctectorLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboLoopProtectorGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                                comboLoopProctectorLamiGSM.Visible = true;
                            }
                            else
                                comboLoopProtectorGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboLoopProctectorColor.Text = DtFillBOM.Rows[i]["color"].ToString();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Stevedore")  //Loop Coverread
                        {
                            checkStevdore.Checked = true;

                            comboStGrm.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboStSize.Text = DtFillBOM.Rows[i]["fabricSize"].ToString().Trim();
                            comboSteveDoreColor.Text = DtFillBOM.Rows[i]["color"].ToString();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Inner Skin")  //Loop Coverread
                        {
                            checkInnerSkin.Checked = true;
                            checkInnerSkinLami.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkInnerSkinLami.Checked = true;
                                comboInnerSkinLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboInnerSkinLamiGSM.Visible = true;
                                comboInnerSkinGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboInnerSkinGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString();
                            comboInnerSkinColor.Text = DtFillBOM.Rows[i]["color"].ToString();
                        }
                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Inner Top")  //Loop Coverread
                        {
                            checkInnerTop.Checked = true;
                            checkInnerTopLami.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkInnerTopLami.Checked = true;
                                comboInnerTopLamiGSM.Visible = true;
                                comboInnerTopLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboInnerTopGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                            }
                            else
                                comboInnerTopGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Trim();
                            comboInnerTopColor.Text = DtFillBOM.Rows[i]["color"].ToString().Trim();
                        }

                        else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Inner Bottom")  //Loop Coverread
                        {
                            checkInnerBottom.Checked = true;
                            checkInnerBottomLami.Checked = false;
                            if (DtFillBOM.Rows[i]["Lami"].ToString() == "Laminated")
                            {
                                checkInnerBottomLami.Checked = true;
                                comboInnerBottomLamiGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[1].Trim();
                                comboInnerBottomGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString().Split('+')[0].Trim();
                                comboInnerBottomLamiGSM.Visible = true;
                            }
                            else
                                comboInnerBottomGSM.Text = DtFillBOM.Rows[i]["GSM"].ToString();
                            comboInnerBottomColor.Text = DtFillBOM.Rows[i]["color"].ToString();
                        }



                        //  addDataRow("Thread", "", "", "", "", "0".ToString(), "0".ToString(), Math.Round((ThreadWt / 100000), 4).ToString(), "", SrNo, ref dataset2);
                        //  TotalKg += Math.Round((ThreadWt / 100000), 4);
                    }
                    #endregion
                    #region BOM2
                    //Database.myreader = Database.GetExecuteReaderCommand("select * from BOM2 WITH(nolock) where PONo ='" +
                    //          textFilePONo.Text + "' and SrNo != 'temp'");
                    //while (Database.myreader.Read())
                    //{
                    //    if (Database.myreader[0].ToString() == "Reinforce Fabric" || Database.myreader[0].ToString() == "Tunnel")
                    //    {
                    //        checkBoxTunnel.Checked = true;
                    //        comboTunnelDesign.SelectedIndex = 0;
                    //        if (Database.myreader[1].ToString().Contains("+"))
                    //        {
                    //            checkTunnelLam.Checked = true;
                    //            string[] str = Database.myreader[1].ToString().Split('+');
                    //            comboTunnelGSM.Text = str[0];
                    //            comboTunnelLamiGSM.Text = str[1];
                    //        }
                    //        else
                    //            comboTunnelGSM.Text = Database.myreader[1].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Buffle")
                    //        _BodyIndex1 = 2;

                    //    //if (Database.myreader[0].ToString() == "Felt")
                    //    //    comboFillerCord.SelectedIndex = 3;

                    //    if (Database.myreader[0].ToString() == "Top Flap")
                    //    {
                    //        checktopflap.Checked = true;
                    //        if (Database.myreader[1].ToString().Contains("+"))
                    //        {
                    //            checkTopFlapLami.Checked = true;
                    //            string[] str = Database.myreader[1].ToString().Split('+');
                    //            comboBoxtopflapgsm.Text = str[0];
                    //            comboTopflapLamiGsm.Text = str[1];
                    //        }
                    //        else
                    //            comboBoxtopflapgsm.Text = Database.myreader[1].ToString();

                    //        comboTopFlapColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Ancillay Loop")
                    //    {
                    //        checkAncerieLoop.Checked = true;
                    //        comboAncerieGrm.Text = Database.myreader[1].ToString();
                    //        comboAncerieColor.Text = Database.myreader[3].ToString();
                    //        AncerieCutLenght = Utility.SafeConvertToDouble(Database.myreader[4].ToString());
                    //        comboAncerieSize.Text = Convert.ToString((AncerieCutLenght - 10) / 2);
                    //    }

                    //    if (Database.myreader[0].ToString() == "Bottom Flap")
                    //    {
                    //        checkBottomflap.Checked = true;
                    //        if (Database.myreader[1].ToString().Contains("+"))
                    //        {
                    //            checkBottomFlapLami.Checked = true;
                    //            string[] str = Database.myreader[1].ToString().Split('+');
                    //            comboBottomflapGSM.Text = str[0];
                    //            comboBottomflapLamiGSM.Text = str[1];
                    //        }
                    //        else
                    //            comboBottomflapGSM.Text = Database.myreader[1].ToString();

                    //        comboBottomFlapColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Top Rope")
                    //    {
                    //        checkTopRope.Checked = true;
                    //        comboTopRopeGrms.Text = Database.myreader[1].ToString();
                    //        comboTopRopeColor.Text = Database.myreader[3].ToString();
                    //    }
                    //    if (Database.myreader[0].ToString() == "Bottom Rope")
                    //    {
                    //        checkBottomRope.Checked = true;
                    //        comboBottomRopeGrms.Text = Database.myreader[1].ToString();
                    //        comboBottomRopeColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "TopSpout Rope")
                    //    {
                    //        checkSpoutRope.Checked = true;
                    //        comboTopSpoutRopeGrm.Text = Database.myreader[1].ToString();
                    //        comboTopSpoutRopeColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "BottomSpout Rope")
                    //    {
                    //        checkBottomSpoutRope.Checked = true;
                    //        comboBottomSpoutRopeGrm.Text = Database.myreader[1].ToString();
                    //        comboBottomSpoutRopeColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Top Hook")
                    //    {
                    //        checkTopFlapHook.Checked = true;
                    //        comboTopflapHookGrm.Text = Database.myreader[1].ToString();
                    //        comboTopHookColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Bottom Hook")
                    //    {
                    //        checkBottomFlapHook.Checked = true;
                    //        comboBottomFlapHookGrm.Text = Database.myreader[1].ToString();
                    //        comboBottomHookColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Top Tie")
                    //    {
                    //        checkTopTie.Checked = true;
                    //        comboTopTieGrms.Text = Database.myreader[1].ToString();
                    //        comboTopTieColor.Text = Database.myreader[3].ToString();
                    //    }
                    //    if (Database.myreader[0].ToString() == "Bottom Tie")
                    //    {
                    //        checkBottomTie.Checked = true;
                    //        comboBottomTieGrm.Text = Database.myreader[1].ToString();
                    //        comboBottomTieColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Duffle/Skrit")
                    //    {
                    //        comboTopType.SelectedIndex = 2;
                    //        if (Database.myreader[1].ToString().Contains("+"))
                    //        {
                    //            checkBoxTopLam.Checked = true;
                    //            string[] str = Database.myreader[1].ToString().Split('+');
                    //            comboBoxTopGSM.Text = str[0];
                    //            comboTopLamiGSM.Text = str[1].Trim();
                    //        }
                    //        else
                    //            comboBoxTopGSM.Text = Database.myreader[1].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Top Band")
                    //    {
                    //        checkBoxTopBand.Checked = true;
                    //        combotopbandgrm.Text = Database.myreader[1].ToString();
                    //        comboTopBandColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Loop Cover")
                    //    {
                    //        checkLoopCover.Checked = true;
                    //        if (Database.myreader[1].ToString().Contains("+"))
                    //        {
                    //            checkLoopCoverLami.Checked = true;
                    //            string[] str = Database.myreader[1].ToString().Split('+');
                    //            comboLoopCoverGSM.Text = str[0];
                    //            comboLoopCoverLamiGSM.Text = str[1];
                    //        }
                    //        else
                    //            comboLoopCoverGSM.Text = Database.myreader[1].ToString();
                    //        comboLoopCoverColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Inner Box")
                    //    {
                    //        checkInnerBox.Checked = true;
                    //        if (Database.myreader[1].ToString().Contains("+"))
                    //        {
                    //            checkInnerBoxLami.Checked = true;
                    //            string[] str = Database.myreader[1].ToString().Split('+');
                    //            comboInnerBoxGSM.Text = str[0];
                    //            comboInnerBoxLamiGSM.Text = str[1];
                    //        }
                    //        else
                    //            comboInnerBoxGSM.Text = Database.myreader[1].ToString();
                    //        comboInnerBoxColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Loop Protector")
                    //    {
                    //        checkLoopProtector.Checked = true;
                    //        if (Database.myreader[1].ToString().Contains("+"))
                    //        {
                    //            checkLoopProcLami.Checked = true;
                    //            string[] str = Database.myreader[1].ToString().Split('+');
                    //            comboLoopProtectorGSM.Text = str[0];
                    //            comboLoopProctectorLamiGSM.Text = str[1];
                    //        }
                    //        else
                    //            comboLoopProtectorGSM.Text = Database.myreader[1].ToString();
                    //        comboLoopProctectorColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "Stevedore")
                    //    {
                    //        checkStevdore.Checked = true;
                    //        comboStGrm.Text = Database.myreader[1].ToString();
                    //        comboSteveDoreColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "InnerSkin")
                    //    {
                    //        checkInnerSkin.Checked = true;
                    //        if (Database.myreader[1].ToString().Contains("+"))
                    //        {
                    //            checkInnerSkinLami.Checked = true;
                    //            string[] str = Database.myreader[1].ToString().Split('+');
                    //            comboInnerSkinGSM.Text = str[0];
                    //            comboInnerSkinLamiGSM.Text = str[1];
                    //        }
                    //        else
                    //            comboInnerSkinGSM.Text = Database.myreader[1].ToString();
                    //        comboInnerSkinColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "InnerTop")
                    //    {
                    //        checkInnerTop.Checked = true;
                    //        if (Database.myreader[1].ToString().Contains("+"))
                    //        {
                    //            checkInnerTopLami.Checked = true;
                    //            string[] str = Database.myreader[1].ToString().Split('+');
                    //            comboInnerTopGSM.Text = str[0];
                    //            comboInnerTopLamiGSM.Text = str[1];
                    //        }
                    //        else
                    //            comboInnerTopGSM.Text = Database.myreader[1].ToString();
                    //        comboInnerTopColor.Text = Database.myreader[3].ToString();
                    //    }

                    //    if (Database.myreader[0].ToString() == "InnerBottom")
                    //    {
                    //        checkInnerBottom.Checked = true;
                    //        if (Database.myreader[1].ToString().Contains("+"))
                    //        {
                    //            checkInnerBottom.Checked = true;
                    //            string[] str = Database.myreader[1].ToString().Split('+');
                    //            comboInnerBottomGSM.Text = str[0];
                    //            comboInnerBottomLamiGSM.Text = str[1];
                    //        }
                    //        else
                    //            comboInnerBottomGSM.Text = Database.myreader[1].ToString();
                    //        comboInnerBottomColor.Text = Database.myreader[3].ToString();
                    //    }

                    //}
                    //Database.myreader.Close();
                    #endregion
                    #region BOM3

                    Database.myreader = Database.GetExecuteReaderCommand("select RF,Thread,Hiracle,HiracleTop,HIracleBottom,ThreadBuffleSeam,ThreadNeedle,DropLoop,TillTheBottom,LoopLength,isfillercord,Fillercord," +
                        " fillercordtop,fillercordbottom,fillercordtopspout,fillercordbottomspout,fillercordbody,fillercordbuffle,ThreadColor,threadtype,threaddenier,toptypes,felttop,feltbottom,felttopspout,feltbottomspout," +
                        " feltundertheloop,fsno,dsno,dsno1,dsno2,feltbody,StartSewnBaseHt,stevdoreno,topno,topflapno,bottomflapno,bottomloopno,BuffleType, BuffleSideA, BuffleSideB,SubBuffleType,feltMFWeb,DoubleFoldBody,DoubleFoldTop," +
                        " DoubleFoldBottom,DoubleFoldBottomSpout,DoubleFoldBottomSpout2,BoxSpoutConical,IsTopVelcro,TopVelcro,IsTopHoseSlider,TopHoseSlider,IsCableTie,CableTie,bottomgsm1,Isbottomlam1,BottomSubTypeLamiGSM,Isbottomtieextra,Isbottomvelcro," +
                        " bottomvelcro,IsBottomhoseslider,Bottomhoseslider,IsBoxbottomwiretie,bottomwiretie,Isbottomcabletie,bottomcabletie,BottomConicalHeight,BottomSubTypeRemarks,BottomSubTypeColor," +
                        " IsBS1bottomtieextra1,IsBS1bottomvelcro1,BS1bottomvelcro1,IsBS1Bottomhoseslider1,BS1Bottomhoseslider1,IsBS1bottomwiretie1,BS1bottomwiretie1,IsBS1bottomcabletie1,BS1bottomcabletie1,BS1SkirtHeight1,BS1Bottomrem1,BS1bottomgsm2,IsBS1bottomlam2,BS1BottomLamiGSM1,BS1BottomNo1,BS1BottomColor1,BS1SpoutRope1,BS1SpoutRopeSize1,BS1SpoutRopeNo1,BS1SpoutRopeColor1,BS1spoutroperemarks1,BS1TieGrm1,BS1TieSize1,BS1TieCutSize1,BS1SpoutTieNo1,ISBottomSpoutRope1 , " +
                        " BS2bottomgsm4,IsBS2bottomlam4,BS2LamiGSM2,BS2No2,BS2Color2,BS2rem2,IsBS2tieextra2,IsBS2velcro2,BS2velcro2,IsBS2hoseslider2,BS2hoseslider2,IsBS2wiretie2,BS2wiretie2,IsBS2cabletie2,BS2cabletie2,BS2SkirtHeight2,BS2SpoutRope2,BS2SpoutRopeSize2,BS2SpoutRopeNo2,BS2TieSize2,BS2TieCutSize2,BS2TieNo2 ,ISlabel,Isblock,blocknos, " +
                        " InnerSkinExtraCutLenght,InnerTopSize,InnerBottomSize,InnerSkinSize,InnerTopExtra,InnerBottomExtra,InnerTopDia,InnerBottomDia,InnerTopHeight,InnerBottomheight,LoopProtectorSize,IsMFWebTop,IsMFWebBottom,IsMFWebTopSpout,IsMFWebBottomSpout,IsMFWebBody,BottomFlapCutLenght, " +
                        " ISTopFlapDRing,BottomBoxtopflapdring,IsBottomFlapDRing,Boxbottomflapdring,SpoutRopeType," +
                        " IsExtraLabel,ExtraLabelNo,ExtralabelL,ExtralabelW,ExtraLabelMicron,IsBoXExtraLabel,ExtraLabelLam,ExtraLabeltype,Extralabelsubtype,ExtralabelL1,ExtralabelW1,ExtraLabelMicron1,IsBoXExtraLabel1,ExtraLabelLam1,ExtraLabeltype1,Extralabelsubtype1,ExtralabelL2,ExtralabelW2,ExtraLabelMicron2,IsBoXExtraLabel2,ExtraLabelLam2,ExtraLabeltype2,Extralabelsubtype2,ExtralabelL3,ExtralabelW3,ExtraLabelMicron3,IsBoXExtraLabel3,ExtraLabelLam3,ExtraLabeltype3,Extralabelsubtype3, " +
                        " BottomSpoutRopeNo,BottomSpoutTieNo,Boxlabelnos,TopHookNo,BottomHookNo,NosAncerieLoop,TopTieNo,TopRopeNo,BottomTieNo,BottomRopeNo,TopSpoutTieIRISNo,BottomSpoutTieIRISNo, " +
                        " LoopCoverNo,StNo,TopSpoutRopeNo,TopSpoutTieNo, " +
                        " TopTieCutSizes,TopRopeCutSizes,BottomTieCutSize,BottomRopeCutSizes " +
                        " from BOM3 WITH(nolock) where PONo ='" + textFilePONo.Text + "'  ");
                    if (Database.myreader.Read())
                    {
                        comboTopType.Text = Database.myreader["Toptypes"].ToString();
                        checkBoxRF.Checked = (Database.myreader["RF"].ToString() == "Yes" ? true : false);
                        checkThread.Checked = (Database.myreader["Thread"].ToString() == "Yes" ? true : false);
                        checkHiracle.Checked = (Database.myreader["Hiracle"].ToString() == "Yes" ? true : false);
                        checkFeltTop.Checked = (Database.myreader["feltTop"].ToString() == "Yes" ? true : false);
                        checkFeltBottom.Checked = (Database.myreader["feltBottom"].ToString() == "Yes" ? true : false);
                        checkFeltTopSpout.Checked = (Database.myreader["feltTopspout"].ToString() == "Yes" ? true : false);
                        checkFeltBottomSpout.Checked = (Database.myreader["feltbottomspout"].ToString() == "Yes" ? true : false);
                        checkFeltUnderTheLoop.Checked = (Database.myreader["feltundertheloop"].ToString() == "Yes" ? true : false);
                        checkFeltBody.Checked = (Database.myreader["feltBody"].ToString() == "Yes" ? true : false);
                        checkFeltMfwebbing.Checked = (Database.myreader["feltMFWeb"].ToString() == "Yes" ? true : false); //19.06.2021

                        #region added three fields 17.06.2021
                        comboBuType.Text = Convert.ToString(Database.myreader["BuffleType"]);
                        txtBuffSideA.Text = Convert.ToString(Database.myreader["BuffleSideA"]);
                        txtBuffSideB.Text = Convert.ToString(Database.myreader["BuffleSideB"]);
                        cmbSubBufType.Text = Convert.ToString(Database.myreader["SubBuffleType"]);

                        checkBoxDoubleFoldBody.Checked = (Database.myreader["DoubleFoldBody"].ToString() == "Yes" ? true : false);
                        checkBoxDoubleFoldTop.Checked = (Database.myreader["DoubleFoldTop"].ToString() == "Yes" ? true : false);
                        checkBoxDoubleFoldBottom.Checked = (Database.myreader["DoubleFoldBottom"].ToString() == "Yes" ? true : false);
                        checkBoxDoubleFoldBottomSpout.Checked = (Database.myreader["DoubleFoldBottomSpout"].ToString() == "Yes" ? true : false);
                        checkBoxDoubleFoldBottomSpout2.Checked = (Database.myreader["DoubleFoldBottomSpout2"].ToString() == "Yes" ? true : false);
                        checkBoxSpoutConical.Checked = (Database.myreader["BoxSpoutConical"].ToString() == "Yes" ? true : false);
                        checkTopVelcro.Checked = (Database.myreader["IsTopVelcro"].ToString() == "Yes" ? true : false);
                        checkTopHoseSlider.Checked = (Database.myreader["IsTopHoseSlider"].ToString() == "Yes" ? true : false);
                        checkBoxCableTie.Checked = (Database.myreader["IsCableTie"].ToString() == "Yes" ? true : false);
                        comboTopVelcro.Text = Convert.ToString(Database.myreader["TopVelcro"]);
                        textBoxCableTie.Text = Convert.ToString(Database.myreader["CableTie"]);

                        //comboBoxbottomgsm1.Text = Convert.ToString(Database.myreader["bottomgsm1"]);
                        //checkBoxbottomlam1.Checked = (Database.myreader["Isbottomlam1"].ToString() == "Yes" ? true : false);
                        //comboBoxBottomSubTypeLamiGSM.Text = Convert.ToString(Database.myreader["BottomSubTypeLamiGSM"]);
                        checkBoxbottomtieextra.Checked = (Database.myreader["Isbottomtieextra"].ToString() == "Yes" ? true : false);
                        checkbottomvelcro.Checked = (Database.myreader["Isbottomvelcro"].ToString() == "Yes" ? true : false);
                        combobottomvelcro.Text = Convert.ToString(Database.myreader["bottomvelcro"]);
                        checkBottomhoseslider.Checked = (Database.myreader["IsBottomhoseslider"].ToString() == "Yes" ? true : false);
                        comboBottomhoseslider.Text = Convert.ToString(Database.myreader["Bottomhoseslider"]);
                        checkBoxbottomwiretie.Checked = (Database.myreader["IsBoxbottomwiretie"].ToString() == "Yes" ? true : false);
                        textBoxbottomwiretie.Text = Convert.ToString(Database.myreader["bottomwiretie"]);
                        checkBoxbottomcabletie.Checked = (Database.myreader["Isbottomcabletie"].ToString() == "Yes" ? true : false);
                        textBoxbottomcabletie.Text = Convert.ToString(Database.myreader["bottomcabletie"]);
                        comboConicalHeight.Text = Convert.ToString(Database.myreader["BottomConicalHeight"]);
                        comboBottomSpoutColor.Text = Convert.ToString(Database.myreader["BottomSubTypeColor"]);
                        textBottomRemarks.Text = Convert.ToString(Database.myreader["BottomSubTypeRemarks"]);
                        #endregion

                        #region 21.09.2021
                        checkBoxbottomtieextra1.Checked = (Database.myreader["IsBS1bottomtieextra1"].ToString() == "Yes" ? true : false);
                        checkbottomvelcro1.Checked = (Database.myreader["IsBS1bottomvelcro1"].ToString() == "Yes" ? true : false);
                        combobottomvelcro1.Text = Convert.ToString(Database.myreader["BS1bottomvelcro1"]);
                        checkBottomhoseslider1.Checked = (Database.myreader["IsBS1Bottomhoseslider1"].ToString() == "Yes" ? true : false);
                        comboBottomhoseslider1.Text = Convert.ToString(Database.myreader["BS1Bottomhoseslider1"]);
                        checkBoxbottomwiretie1.Checked = (Database.myreader["IsBS1bottomwiretie1"].ToString() == "Yes" ? true : false);
                        textBoxbottomwiretie1.Text = Convert.ToString(Database.myreader["BS1bottomwiretie1"]);
                        checkBoxbottomcabletie1.Checked = (Database.myreader["IsBS1bottomcabletie1"].ToString() == "Yes" ? true : false);
                        textBoxbottomcabletie1.Text = Convert.ToString(Database.myreader["BS1bottomcabletie1"]);
                        textSkirtHeight1.Text = Convert.ToString(Database.myreader["BS1SkirtHeight1"]);
                        textBottomrem1.Text = Convert.ToString(Database.myreader["BS1Bottomrem1"]);
                        comboBoxbottomgsm2.Text = Convert.ToString(Database.myreader["BS1bottomgsm2"]);
                        checkBoxbottomlam2.Checked = (Database.myreader["IsBS1bottomlam2"].ToString() == "Yes" ? true : false);
                        comboBottomLamiGSM1.Text = Convert.ToString(Database.myreader["BS1BottomLamiGSM1"]);
                        textBottomNo1.Text = Convert.ToString(Database.myreader["BS1BottomNo1"]);
                        comboBottomColor1.Text = Convert.ToString(Database.myreader["BS1BottomColor1"]);
                        comboBottomSpoutRope1.Text = Convert.ToString(Database.myreader["BS1SpoutRope1"]);
                        comboBottomSpoutRopeSize1.Text = Convert.ToString(Database.myreader["BS1SpoutRopeSize1"]);
                        textBottomSpoutRopeNo1.Text = Convert.ToString(Database.myreader["BS1SpoutRopeNo1"]);
                        comboBottomSpoutRopeColor1.Text = Convert.ToString(Database.myreader["BS1SpoutRopeColor1"]);
                        textBottomspoutroperemarks1.Text = Convert.ToString(Database.myreader["BS1spoutroperemarks1"]);
                        checkBottomSpoutRope1.Checked = (Database.myreader["ISBottomSpoutRope1"].ToString() == "Yes" ? true : false);
                        comboBottomSpoutTieGrm1.Text = Convert.ToString(Database.myreader["BS1TieGrm1"]);
                        comboBottomSpoutTieSize1.Text = Convert.ToString(Database.myreader["BS1TieSize1"]);
                        comboBottomSpoutTieCutSize1.Text = Convert.ToString(Database.myreader["BS1TieCutSize1"]);
                        textBottomSpoutTieNo1.Text = Convert.ToString(Database.myreader["BS1SpoutTieNo1"]);

                        comboBoxbottomgsm4.Text = Convert.ToString(Database.myreader["BS2bottomgsm4"]);
                        checkBoxbottomlam4.Checked = (Database.myreader["IsBS2bottomlam4"].ToString() == "Yes" ? true : false);
                        comboBottomLamiGSM2.Text = Convert.ToString(Database.myreader["BS2LamiGSM2"]);
                        textBottomNo2.Text = Convert.ToString(Database.myreader["BS2No2"]);
                        comboBottomColor2.Text = Convert.ToString(Database.myreader["BS2Color2"]);
                        textBottomrem2.Text = Convert.ToString(Database.myreader["BS2rem2"]);
                        checkBoxbottomtieextra2.Checked = (Database.myreader["IsBS2tieextra2"].ToString() == "Yes" ? true : false);
                        checkbottomvelcro2.Checked = (Database.myreader["IsBS2velcro2"].ToString() == "Yes" ? true : false);
                        combobottomvelcro2.Text = Convert.ToString(Database.myreader["BS2velcro2"]);
                        checkBottomhoseslider2.Checked = (Database.myreader["IsBS2hoseslider2"].ToString() == "Yes" ? true : false);
                        comboBottomhoseslider2.Text = Convert.ToString(Database.myreader["BS2hoseslider2"]);
                        checkBoxbottomwiretie2.Checked = (Database.myreader["IsBS2wiretie2"].ToString() == "Yes" ? true : false);
                        textBoxbottomwiretie2.Text = Convert.ToString(Database.myreader["BS2wiretie2"]);
                        checkBoxbottomcabletie2.Checked = (Database.myreader["IsBS2cabletie2"].ToString() == "Yes" ? true : false);
                        textBoxbottomcabletie2.Text = Convert.ToString(Database.myreader["BS2cabletie2"]);
                        textSkirtHeight2.Text = Convert.ToString(Database.myreader["BS2SkirtHeight2"]);
                        comboBottomSpoutRope2.Text = Convert.ToString(Database.myreader["BS2SpoutRope2"]);
                        comboBottomSpoutRopeSize2.Text = Convert.ToString(Database.myreader["BS2SpoutRopeSize2"]);
                        textBottomSpoutRopeNo2.Text = Convert.ToString(Database.myreader["BS2SpoutRopeNo2"]);
                        comboBottomSpoutTieSize2.Text = Convert.ToString(Database.myreader["BS2TieSize2"]);
                        comboBottomSpoutTieCutSize2.Text = Convert.ToString(Database.myreader["BS2TieCutSize2"]);
                        textBottomSpoutTieNo2.Text = Convert.ToString(Database.myreader["BS2TieNo2"]);

                        checkBoxlabel.Checked = (Database.myreader["ISlabel"].ToString() == "Yes" ? true : false);
                        checkBoxblock.Checked = (Database.myreader["Isblock"].ToString() == "Yes" ? true : false);
                        textBoxblocknos.Text = Convert.ToString(Database.myreader["blocknos"]);
                        textInnerSkinExtraCutLenght.Text = Convert.ToString(Database.myreader["InnerSkinExtraCutLenght"]);
                        comboInnerTopSize.Text = Convert.ToString(Database.myreader["InnerTopSize"]);
                        comboInnerBottomSize.Text = Convert.ToString(Database.myreader["InnerBottomSize"]);
                        comboInnerSkinSize.Text = Convert.ToString(Database.myreader["InnerSkinSize"]);
                        textInnerTopExtra.Text = Convert.ToString(Database.myreader["InnerTopExtra"]);
                        textInnerBottomExtra.Text = Convert.ToString(Database.myreader["InnerBottomExtra"]);
                        comboInnerTopDia.Text = Convert.ToString(Database.myreader["InnerTopDia"]);
                        comboInnerBottomDia.Text = Convert.ToString(Database.myreader["InnerBottomDia"]);
                        comboInnerTopHeight.Text = Convert.ToString(Database.myreader["InnerTopHeight"]);
                        comboInnerBottomheight.Text = Convert.ToString(Database.myreader["InnerBottomheight"]);
                        comboLoopProtector.Text = Convert.ToString(Database.myreader["LoopProtectorSize"]);
                        checkMFWebTop.Checked = (Database.myreader["IsMFWebTop"].ToString() == "Yes" ? true : false);
                        checkMFWebBottom.Checked = (Database.myreader["IsMFWebBottom"].ToString() == "Yes" ? true : false);
                        checkMFWebTopSpout.Checked = (Database.myreader["IsMFWebTopSpout"].ToString() == "Yes" ? true : false);
                        checkMFWebBottomSpout.Checked = (Database.myreader["IsMFWebBottomSpout"].ToString() == "Yes" ? true : false);
                        checkMFWebBody.Checked = (Database.myreader["IsMFWebBody"].ToString() == "Yes" ? true : false);
                        textBottomFlapCutLenght.Text = Convert.ToString(Database.myreader["BottomFlapCutLenght"]);



                        #endregion
                        #region 22.09.2021
                        chkTopFlapDRing.Checked = (Database.myreader["ISTopFlapDRing"].ToString() == "Yes" ? true : false);
                        comboBoxtopflapdring.Text = Convert.ToString(Database.myreader["BottomBoxtopflapdring"]);

                        chkBottomFlapDRing.Checked = (Database.myreader["IsBottomFlapDRing"].ToString() == "Yes" ? true : false);
                        comboBoxbottomflapdring.Text = Convert.ToString(Database.myreader["Boxbottomflapdring"]);
                        comboSpoutRope.Text = Convert.ToString(Database.myreader["SpoutRopeType"]);
                        #endregion
                        #region 23.09.2021
                        checkExtraLabel.Checked = (Database.myreader["IsExtraLabel"].ToString() == "Yes" ? true : false);
                        textExtraLabelNo.Text = Convert.ToString(Database.myreader["ExtraLabelNo"]);
                        textExtralabelL.Text = Convert.ToString(Database.myreader["ExtralabelL"]);
                        textExtralabelW.Text = Convert.ToString(Database.myreader["ExtralabelW"]);
                        comboExtraLabelMicron.Text = Convert.ToString(Database.myreader["ExtraLabelMicron"]);
                        checkBoXExtraLabel.Checked = (Database.myreader["IsBoXExtraLabel"].ToString() == "Yes" ? true : false);
                        comboExtraLabelLam.Text = Convert.ToString(Database.myreader["ExtraLabelLam"]);
                        comboExtraLabeltype.Text = Convert.ToString(Database.myreader["ExtraLabeltype"]);
                        comboExtralabelsubtype.Text = Convert.ToString(Database.myreader["Extralabelsubtype"]);

                        textExtralabelL1.Text = Convert.ToString(Database.myreader["ExtralabelL1"]);
                        textExtralabelW1.Text = Convert.ToString(Database.myreader["ExtralabelW1"]);
                        comboExtraLabelMicron1.Text = Convert.ToString(Database.myreader["ExtraLabelMicron1"]);
                        checkBoXExtraLabel1.Checked = (Database.myreader["IsBoXExtraLabel1"].ToString() == "Yes" ? true : false);
                        comboExtraLabelLam1.Text = Convert.ToString(Database.myreader["ExtraLabelLam1"]);
                        comboExtraLabeltype1.Text = Convert.ToString(Database.myreader["ExtraLabeltype1"]);
                        comboExtralabelsubtype1.Text = Convert.ToString(Database.myreader["Extralabelsubtype1"]);

                        textExtralabelL2.Text = Convert.ToString(Database.myreader["ExtralabelL2"]);
                        textExtralabelW2.Text = Convert.ToString(Database.myreader["ExtralabelW2"]);
                        comboExtraLabelMicron2.Text = Convert.ToString(Database.myreader["ExtraLabelMicron2"]);
                        checkBoXExtraLabel2.Checked = (Database.myreader["IsBoXExtraLabel2"].ToString() == "Yes" ? true : false);
                        comboExtraLabelLam2.Text = Convert.ToString(Database.myreader["ExtraLabelLam2"]);
                        comboExtraLabeltype2.Text = Convert.ToString(Database.myreader["ExtraLabeltype2"]);
                        comboExtralabelsubtype2.Text = Convert.ToString(Database.myreader["Extralabelsubtype2"]);

                        textExtralabelL3.Text = Convert.ToString(Database.myreader["ExtralabelL3"]);
                        textExtralabelW3.Text = Convert.ToString(Database.myreader["ExtralabelW3"]);
                        comboExtraLabelMicron3.Text = Convert.ToString(Database.myreader["ExtraLabelMicron3"]);
                        checkBoXExtraLabel3.Checked = (Database.myreader["IsBoXExtraLabel3"].ToString() == "Yes" ? true : false);
                        comboExtraLabelLam3.Text = Convert.ToString(Database.myreader["ExtraLabelLam3"]);
                        comboExtraLabeltype3.Text = Convert.ToString(Database.myreader["ExtraLabeltype3"]);
                        comboExtralabelsubtype3.Text = Convert.ToString(Database.myreader["Extralabelsubtype3"]);

                        #endregion
                        #region 29.09.2021

                        textBottomSpoutRopeNo.Text = Convert.ToString(Database.myreader["BottomSpoutRopeNo"]);
                        if (textBottomSpoutRopeNo.Text.Length == 0)
                            textBottomSpoutRopeNo.Text = "1";
                        textBottomSpoutTieNo.Text = Convert.ToString(Database.myreader["BottomSpoutTieNo"]);
                        if (textBottomSpoutTieNo.Text.Length == 0)
                            textBottomSpoutTieNo.Text = "1";

                        textBoxlabelnos.Text = Convert.ToString(Database.myreader["Boxlabelnos"]);
                        if (textBoxlabelnos.Text.Length == 0)
                            textBoxlabelnos.Text = "1";

                        textTopHookNo.Text = Convert.ToString(Database.myreader["TopHookNo"]);
                        if (textTopHookNo.Text.Length == 0)
                            textTopHookNo.Text = "1";

                        textBottomHookNo.Text = Convert.ToString(Database.myreader["BottomHookNo"]);
                        if (textBottomHookNo.Text.Length == 0)
                            textBottomHookNo.Text = "1";

                        textNosAncerieLoop.Text = Convert.ToString(Database.myreader["NosAncerieLoop"]);
                        if (textNosAncerieLoop.Text.Length == 0)
                            textNosAncerieLoop.Text = "1";

                        textTopTieNo.Text = Convert.ToString(Database.myreader["TopTieNo"]);
                        if (textTopTieNo.Text.Length == 0)
                            textTopTieNo.Text = "1";

                        textTopRopeNo.Text = Convert.ToString(Database.myreader["TopRopeNo"]);
                        if (textTopRopeNo.Text.Length == 0)
                            textTopRopeNo.Text = "1";

                        textBottomTieNo.Text = Convert.ToString(Database.myreader["BottomTieNo"]);
                        if (textBottomTieNo.Text.Length == 0)
                            textBottomTieNo.Text = "1";

                        textBottomRopeNo.Text = Convert.ToString(Database.myreader["BottomRopeNo"]);
                        if (textBottomRopeNo.Text.Length == 0)
                            textBottomRopeNo.Text = "1";

                        textTopSpoutTieIRISNo.Text = Convert.ToString(Database.myreader["TopSpoutTieIRISNo"]);
                        if (textTopSpoutTieIRISNo.Text.Length == 0)
                            textTopSpoutTieIRISNo.Text = "1";

                        textBottomSpoutTieIRISNo.Text = Convert.ToString(Database.myreader["BottomSpoutTieIRISNo"]);
                        if (textBottomSpoutTieIRISNo.Text.Length == 0)
                            textBottomSpoutTieIRISNo.Text = "1";

                        textLoopCoverNo.Text = Convert.ToString(Database.myreader["LoopCoverNo"]);
                        if (textLoopCoverNo.Text.Length == 0)
                            textLoopCoverNo.Text = "1";
                        textStNo.Text = Convert.ToString(Database.myreader["StNo"]);
                        if (textStNo.Text.Length == 0)
                            textStNo.Text = "1";
                        textTopSpoutRopeNo.Text = Convert.ToString(Database.myreader["TopSpoutRopeNo"]);
                        if (textTopSpoutRopeNo.Text.Length == 0)
                            textTopSpoutRopeNo.Text = "1";
                        textTopSpoutTieNo.Text = Convert.ToString(Database.myreader["TopSpoutTieNo"]);
                        if (textTopSpoutTieNo.Text.Length == 0)
                            textTopSpoutTieNo.Text = "1";

                        #endregion
                        #region 07.10.2021

                        comboTopTieCutSizes.Text = Convert.ToString(Database.myreader["TopTieCutSizes"]);
                        comboTopRopeCutSizes.Text = Convert.ToString(Database.myreader["TopRopeCutSizes"]);
                        comboBottomTieCutSize.Text = Convert.ToString(Database.myreader["BottomTieCutSize"]);
                        comboBottomRopeCutSizes.Text = Convert.ToString(Database.myreader["BottomRopeCutSizes"]);
                        #endregion


                        textFSNo.Text = Convert.ToString(Database.myreader["fsno"]);
                        if (Utility.SafeConvertToDouble(textFSNo.Text) == 0)
                            textFSNo.Text = "1";
                        textDSNo.Text = Convert.ToString(Database.myreader["dsno"]);
                        if (Utility.SafeConvertToDouble(textDSNo.Text) == 0)
                            textDSNo.Text = "1";
                        textDSNo1.Text = Database.myreader["dsno1"].ToString();
                        if (Utility.SafeConvertToDouble(textDSNo1.Text) == 0)
                            textDSNo1.Text = "1";
                        textDSNo2.Text = Database.myreader["dsno2"].ToString();
                        if (Utility.SafeConvertToDouble(textDSNo2.Text) == 0)
                            textDSNo1.Text = "1";
                        textTopNo.Text = Database.myreader["topno"].ToString();
                        if (Utility.SafeConvertToDouble(textTopNo.Text) == 0)
                            textTopNo.Text = "1";
                        checkHiracleTop.Checked = (Database.myreader["HiracleTop"].ToString() == "Yes" ? true : false);
                        checkHiracleBottom.Checked = (Database.myreader["HIracleBottom"].ToString() == "Yes" ? true : false);
                        comboThreadBuffleSeam.Text = (Database.myreader["ThreadBuffleSeam"].ToString());
                        comboThreadNeedle.Text = (Database.myreader["ThreadNeedle"].ToString());
                        checkBoxDropLoop.Checked = (Database.myreader["DropLoop"].ToString() == "Yes" ? true : false);
                        checkLoopTillBottom.Checked = (Database.myreader["TillTheBottom"].ToString() == "Yes" ? true : false);
                        textLoopLenght.Text = Database.myreader["LoopLength"].ToString();
                        checkFillerCord.Checked = (Database.myreader["isfillercord"].ToString() == "Yes" ? true : false);
                        comboFillerCordTop.Text = Database.myreader["Fillercord"].ToString();
                        checkFillerTop.Checked = (Database.myreader["fillercordtop"].ToString() == "Yes" ? true : false);
                        checkFillerBottom.Checked = (Database.myreader["fillercordbottom"].ToString() == "Yes" ? true : false);
                        checkFillerTopSpout.Checked = (Database.myreader["fillercordtopspout"].ToString() == "Yes" ? true : false);
                        checkFillerBottomSpout.Checked = (Database.myreader["fillercordbottomspout"].ToString() == "Yes" ? true : false);
                        CheckFillerBody.Checked = (Database.myreader["fillercordbody"].ToString() == "Yes" ? true : false);
                        comboBuffleSeam.Text = Database.myreader["fillercordbuffle"].ToString();
                        comboThreadColor.Text = Database.myreader["ThreadColor"].ToString();
                        comboThreadType.Text = Database.myreader["threadtype"].ToString();
                        textThreadDenier.Text = Database.myreader["threaddenier"].ToString();
                        textStNo.Text = Database.myreader["stevdoreno"].ToString();
                        textStartSewnBaseHt.Text = Database.myreader["StartSewnBaseHt"].ToString();
                        textBoxtopflapnosflap.Text = Database.myreader["topflapno"].ToString();
                        txtBottomFlap.Text = Database.myreader["bottomflapno"].ToString();
                        textBottomLoopNo.Text = Database.myreader["bottomloopno"].ToString();

                        if (txtBottomFlap.Text == "")
                            txtBottomFlap.Text = "1";

                        if (textBoxtopflapnosflap.Text == "")
                            textBoxtopflapnosflap.Text = "1";
                        if (textBottomLoopNo.Text == "")
                            textBottomLoopNo.Text = "1";


                    }
                    Database.myreader.Close();

                    Database.myreader = Database.GetExecuteReaderCommand(" select fillercordtoptype,fillercordbottomtype,fillercordFSType,FillercordDStype,fillercordbodytype,fillercordbuffletype,fillercordbuffle1 from BOM3 WITH(nolock) where PONo ='" +
                             textFilePONo.Text + "' and SrNo != 'temp'");
                    if (Database.myreader.Read())
                    {
                        comboFillerCordTop.Text = Database.myreader[0].ToString();
                        comboFillerCordBottom.Text = Database.myreader[1].ToString();
                        comboFillerCordTopS.Text = Database.myreader[2].ToString();
                        comboFillerCordBottomS.Text = Database.myreader[3].ToString();
                        comboFillerCordBody.Text = Database.myreader[4].ToString();
                        comboFillerCordBuffle.Text = Database.myreader[5].ToString();

                        if (Database.myreader[6].ToString() == "yes")
                            checkFillerBuffle.Checked = true;
                        else
                            checkFillerBuffle.Checked = false;
                        
                    }
                    Database.myreader.Close();


                    Database.myreader = Database.GetExecuteReaderCommand(" select fsedgehaming,dsedgehaming,docflap,docflapsize,mfwebbingbuffle from BOM3 WITH(nolock) where PONo ='" +
                             textFilePONo.Text + "' and SrNo != 'temp'");
                    if (Database.myreader.Read())
                    {
                        
                        if (Database.myreader[0].ToString() == "yes")
                            checkTopEdgeHemming.Checked = true;
                        else
                            checkTopEdgeHemming.Checked = false;

                        if (Database.myreader[1].ToString() == "yes")
                            checkBottomEdgeHemming.Checked = true;
                        else
                            checkBottomEdgeHemming.Checked = false;

                        if (Database.myreader[2].ToString() == "yes")
                            checkDocFlap.Checked = true;
                        else
                            checkDocFlap.Checked = false;

                        textDocFlapsize.Text = Database.myreader[3].ToString();

                        if (Database.myreader[4].ToString() == "yes")
                            checkMFBuffle.Checked = true;
                        else
                            checkMFBuffle.Checked = false;


                    }
                    Database.myreader.Close();



                    Database.myreader = Database.GetExecuteReaderCommand("select docunit,docl,docw from BOM1 WITH(nolock) where FilePONo ='" +
                               textFilePONo.Text + "' and SrNo != 'temp'");
                    if (Database.myreader.Read())
                    {
                        comboDocUnit.Text = Database.myreader[0].ToString();
                        textDocL.Text = Database.myreader[1].ToString();
                        textDocW.Text = Database.myreader[2].ToString();
                    }
                    Database.myreader.Close();

                    Database.myreader = Database.GetExecuteReaderCommand("select doc1unit,docl1,docw1 from BOM1 WITH(nolock) where FilePONo ='" +
                               textFilePONo.Text + "' and SrNo != 'temp'");
                    if (Database.myreader.Read())
                    {
                        comboDoc1Unit.Text = Database.myreader[0].ToString();
                        textDoc1L.Text = Database.myreader[1].ToString();
                        textDoc1W.Text = Database.myreader[2].ToString();
                    }
                    Database.myreader.Close();

                    Database.myreader = Database.GetExecuteReaderCommand("select doc2unit,docl2,docw2 from BOM1 WITH(nolock) where FilePONo ='" +
                               textFilePONo.Text + "' and SrNo != 'temp'");
                    if (Database.myreader.Read())
                    {
                        comboDoc2Unit.Text = Database.myreader[0].ToString();
                        textDoc2L.Text = Database.myreader[1].ToString();
                        textDoc2W.Text = Database.myreader[2].ToString();
                    }
                    Database.myreader.Close();


                    #endregion
                    Database.Closeconnection();
                    //PopulateData("", BOMNo);
                    comboSpoutDia.Text = fsDia;
                    comboSpoutHeight.Text = fsHt;
                    comboLoopL.Text = LoopLength;
                    comboBoxduffleskirtheight.Text = DuffleHT;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }


        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {

            try
            {
                if (FilePONo != "" && FilePONo != textFilePONo.Text)
                {
                    MessageBox.Show("You can't update. You have made changes in Qtn No.");
                    return;
                }
                int count = 0;
                for (int i = 0; i < dgapprovallist.Rows.Count - 1; i++)
                {
                    if (dgapprovallist.Rows[i].Cells[0].FormattedValue.ToString() == "True")
                    {
                        count++;
                    }
                }

                if (count == 0)
                {
                    MessageBox.Show("Please Select Atleast one item from Approval List");
                    return;
                }

                #region Approved BOM or Not
                if (Database.OpenConnection(Utility.ProductionConnectionString))
                {
                    bool blnAppBOM = false;
                    Database.myreader = Database.GetExecuteReaderCommand("select * from approvebom where filepono = '" + textFilePONo.Text + "'");
                    while (Database.myreader.Read())
                    {
                        if (Convert.ToString(Database.myreader["Status"]).ToLower() == "approved")
                        {
                            blnAppBOM = true;
                        }
                        else
                        {
                            blnAppBOM = false;
                        }
                    }
                    Database.myreader.Close();
                    if (blnAppBOM)
                    {
                        MessageBox.Show("BOM is already Approved.\nYou can't update BOM");
                        return;
                    }
                }
                #endregion

                if (Database.OpenConnection(Utility.ProductionConnectionString))
                {
                    DialogResult dialog = MessageBox.Show("Do you want to update save PONo " + textFilePONo.Text, "Update",
                             MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (dialog.CompareTo(DialogResult.Yes) == 0)
                    {

                        FilePONo = textFilePONo.Text;
                        CompanyName = comboPartyName.Text;
                        string SrNo = "";
                        SrNo = Convert.ToString(System.Guid.NewGuid());
                        IsTemp = false;
                        btnupdateclick = true;
                        //Database.BeginTransaction();
                        //Database.GetExecuteNonQueryCommand("insert into BOM1_delete select * from bom1 where FilePONo = '" + textFilePONo.Text + "'");

                        //Database.GetExecuteNonQueryCommand("insert into BOM_delete select heading,gsm,lami,color,fabricsize,cutsize,totalmtr,totalkg,partyname,pono,srno,modifydate,remarks,gpm,warpdenier,warptape,warpgsm,warpkg,weftdenier,wefttape,weftgsm,weftkg from bom where PONo = '" + textFilePONo.Text + "'");

                        //Database.GetExecuteNonQueryCommand("insert into BOM2_delete select * from bom2 where PONo = '" + textFilePONo.Text + "'");
                        //Database.GetExecuteNonQueryCommand("insert into BOM3_delete select * from bom3 where PONo = '" + textFilePONo.Text + "'");



                        //Database.GetExecuteNonQueryCommand("delete from BOM1 where FilePONo = '" + textFilePONo.Text + "'");
                        //Database.GetExecuteNonQueryCommand("delete from BOM2 where PONo = '" + textFilePONo.Text + "'");
                        //Database.GetExecuteNonQueryCommand("delete from  BOM3 where PONo = '" + textFilePONo.Text + "'");
                        //Database.GetExecuteNonQueryCommand("delete from  BOM where PONo = '" + textFilePONo.Text + "'");

                        print(SrNo);

                        btnupdateclick = false;
                        // Database.CommitTransaction();

                        #region CommentedCode

                        //bool IsError = false;
                        //TotalKg = 0;
                        //if (textBodyL.Text == "")
                        //    MessageBox.Show("Please Enter value in Body Lenght");
                        //else if (textBodyW.Text == "")
                        //    MessageBox.Show("Please Enter value in Body Width");
                        //else if (textBodyH.Text == "")
                        //    MessageBox.Show("Please Enter value in Body Height");
                        //else if (textQty.Text == "")
                        //    MessageBox.Show("Please Enter value in Qty");
                        //else if (textFilePONo.Text == "")
                        //    MessageBox.Show("Please enter value in File PONo");

                        //else if (Database.OpenConnection(Utility.ProductionConnectionString))
                        //{


                        //    //Database.GetExecuteNonQueryCommand("delete from BOM1 where srno = 'temp'");
                        //    //Database.GetExecuteNonQueryCommand("delete from BOM2 where srno = 'temp'");



                        //    IsTemp = false;
                        //    try
                        //    {

                        //        BodyWtFormula();

                        //        BodyWt = BodyWt / 10000000;
                        //        BodyWt = Math.Round(BodyWt, 4);
                        //        if ((_BodyIndex1 == 2 && (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 1))
                        //                 || _BodyIndex1 == 3)
                        //        {
                        //            if (textBodyL.Text == textBodyW.Text)
                        //            {
                        //                BodyTotalMtr = ((BodyCutLenght / 100) * _BagQty * 4)
                        //                      + .1 * ((BodyCutLenght / 100) * _BagQty * 4);
                        //                BodyTotalMtr = Math.Round(BodyTotalMtr, 4);
                        //            }
                        //            else
                        //            {
                        //                BodyTotalMtr = ((BodyCutLenght / 100) * _BagQty * 2)
                        //                      + .1 * ((BodyCutLenght / 100) * _BagQty * 2);
                        //                BodyTotalMtr = Math.Round(BodyTotalMtr, 4);
                        //            }

                        //        }
                        //        else
                        //        {
                        //            BodyTotalMtr = ((BodyCutLenght / 100) * _BagQty)
                        //                  + .1 * ((BodyCutLenght / 100) * _BagQty);
                        //            BodyTotalMtr = Math.Round(BodyTotalMtr, 4);
                        //        }


                        //        if (checkSide.Checked)
                        //        {
                        //            SideWtFormula();
                        //            if (_BodyIndex1 == 1) // circular
                        //                SideWt = BodyWt;
                        //            else
                        //                SideWt = SideWt / 10000000;
                        //            SideWt = Math.Round(SideWt, 4);
                        //            if (_BodyIndex1 == 0) //UPanel 
                        //            {
                        //                SideTotalMtr = ((SideCutLenght / 100) * _BagQty * 2)
                        //                      + .1 * ((SideCutLenght / 100) * _BagQty * 2);
                        //                SideTotalMtr = Math.Round(SideTotalMtr, 4);
                        //            }
                        //            else if (_BodyIndex1 == 3 || _BodyIndex1 == 4) // 4 Panel,Tube + Corner
                        //            {
                        //                if (textBodyL.Text == textBodyW.Text)
                        //                {
                        //                    SideTotalMtr = ((SideCutLenght / 100) * _BagQty * 4)
                        //                         + .1 * ((SideCutLenght / 100) * _BagQty * 4);
                        //                    SideTotalMtr = Math.Round(SideTotalMtr, 4);
                        //                }
                        //                else
                        //                {
                        //                    SideTotalMtr = ((SideCutLenght / 100) * _BagQty * 2)
                        //                         + .1 * ((SideCutLenght / 100) * _BagQty * 2);
                        //                    SideTotalMtr = Math.Round(SideTotalMtr, 4);
                        //                }
                        //            }
                        //            else if (_BodyIndex1 == 2) // Buffle
                        //            {
                        //                if (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 1)
                        //                {
                        //                    SideTotalMtr = ((SideCutLenght / 100) * _BagQty * 2)
                        //                        + .1 * ((SideCutLenght / 100) * _BagQty * 2);
                        //                    SideTotalMtr = Math.Round(SideTotalMtr, 4);
                        //                }
                        //                else
                        //                {
                        //                    SideTotalMtr = ((SideCutLenght / 100) * _BagQty * 4)
                        //                    + .1 * ((SideCutLenght / 100) * _BagQty * 4);
                        //                    SideTotalMtr = Math.Round(SideTotalMtr, 4);
                        //                }
                        //            }
                        //            else if (_BodyIndex1 == 12) // Double Layer Tunnel Lift Loop Bag
                        //            {
                        //                SideTotalMtr = ((SideCutLenght / 100) * _BagQty * 2)
                        //                    + .1 * ((SideCutLenght / 100) * _BagQty * 2);
                        //                SideTotalMtr = Math.Round(SideTotalMtr, 4);
                        //            }
                        //            else
                        //            {
                        //                SideTotalMtr = ((SideCutLenght / 100) * _BagQty)
                        //                    + .1 * ((SideCutLenght / 100) * _BagQty);
                        //                SideTotalMtr = Math.Round(SideTotalMtr, 4);
                        //            }
                        //        }


                        //        if (checkBoxTop.Checked)
                        //        {
                        //            TopWtFormula();
                        //            FSWtFormula();

                        //            TopWt = TopWt / 10000000;
                        //            TopWt = Math.Round(TopWt, 4);
                        //            TopTotalMtr = ((TopCutLenght / 100) * _BagQty)
                        //                  + .1 * ((TopCutLenght / 100) * _BagQty);
                        //            TopTotalMtr = Math.Round(TopTotalMtr, 4);

                        //            FSWt = FSWt / 10000000;
                        //            FSWt = Math.Round(FSWt, 4);
                        //            FSTotalMtr = ((FSCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble (textFSNo.Text))
                        //                  + .1 * ((FSCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble (textFSNo.Text));
                        //            FSTotalMtr = Math.Round(FSTotalMtr, 4);


                        //        }

                        //        if (checkbottom.Checked)
                        //        {
                        //            BaseWtFormula();
                        //            DSWtFormula();

                        //            BaseWt = BaseWt / 10000000;
                        //            BaseWt = Math.Round(BaseWt, 4);
                        //            BaseTotalMtr = ((BaseCutLenght / 100) * _BagQty)
                        //                  + .1 * ((BaseCutLenght / 100) * _BagQty);
                        //            BaseTotalMtr = Math.Round(BaseTotalMtr, 4);

                        //            DSWt = DSWt / 10000000;
                        //            DSWt = Math.Round(DSWt, 4);
                        //            DSTotalMtr = ((DSCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble (textDSNo.Text))
                        //                  + .1 * ((DSCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble (textDSNo.Text));
                        //            DSTotalMtr = Math.Round(DSTotalMtr, 4);

                        //        }

                        //        if (checkBoxlabel.Checked)
                        //        {
                        //            LabelWtFormula();

                        //            LabelWt = LabelWt / 10000000;
                        //            LabelWt = Math.Round(LabelWt, 4);
                        //            LabelTotalMtr = ((LabelCutLenght / 100) * _BagQty)
                        //                  + .1 * ((LabelCutLenght / 100) * _BagQty);
                        //            LabelTotalMtr = Math.Round(LabelTotalMtr, 4);

                        //        }
                        //        if (checkBoxdocpouch.Checked)
                        //        {
                        //            DocWtFormula();
                        //            DocWt = DocWt / 10000000;
                        //            DocWt = Math.Round(DocWt, 4);
                        //            DocTotalMtr = ((DocCutLenght / 100) * _BagQty)
                        //                  + .1 * ((DocCutLenght / 100) * _BagQty);
                        //            DocTotalMtr = Math.Round(DocTotalMtr, 4);

                        //        }


                        //        if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5)
                        //        {
                        //            DuffleWtFormula();

                        //            DuffleWt = DuffleWt / 10000000;
                        //            DuffleWt = Math.Round(DuffleWt, 4);
                        //            DuffleTotalMtr = ((DuffleCutLenght / 100) * _BagQty)
                        //                  + .1 * ((DuffleCutLenght / 100) * _BagQty);
                        //            DuffleTotalMtr = Math.Round(DuffleTotalMtr, 4);

                        //        }
                        //        if (comboBoxbottomtype.SelectedIndex == 8)
                        //        {
                        //            BottomDuffleWtFormula();

                        //            BottomDuffleWt = BottomDuffleWt / 10000000;
                        //            BottomDuffleWt = Math.Round(BottomDuffleWt, 4);
                        //            BottomDuffleTotalMtr = ((BottomDuffleCutLenght / 100) * _BagQty)
                        //                  + .1 * ((BottomDuffleCutLenght / 100) * _BagQty);
                        //            BottomDuffleTotalMtr = Math.Round(BottomDuffleTotalMtr, 4);

                        //        }
                        //        if (checkSpoutTie.Checked)
                        //        {
                        //            FSTieFormula();

                        //            FSTieWt = FSTieWt / 100000;
                        //            FSTieWt = Math.Round(FSTieWt, 4);
                        //            FSTieTotalMtr = ((FSTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble (textTopSpoutTieNo.Text))
                        //                  + .1 * ((FSTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble (textTopSpoutTieNo.Text));
                        //            FSTieTotalMtr = Math.Round(FSTieTotalMtr, 4);

                        //        }
                        //        if (checkBottomSpoutTie.Checked)
                        //        {
                        //            DSTieFormula();
                        //            DSTieWt = DSTieWt / 100000;
                        //            DSTieWt = Math.Round(DSTieWt, 4);
                        //            DSTieTotalMtr = ((DSTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble (textBottomSpoutTieNo.Text))
                        //                  + .1 * ((DSTieCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble (textBottomSpoutTieNo.Text));
                        //            DSTieTotalMtr = Math.Round(DSTieTotalMtr, 4);
                        //        }

                        //        if (checkBoxLoop.Checked)
                        //        {
                        //            LoopWtFormula();
                        //            LoopWt = LoopWt / 100000;
                        //            LoopWt = Math.Round(LoopWt, 4);
                        //            LoopTotalMtr = ((LoopCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble (textLoopNo.Text))
                        //                  + .1 * ((LoopCutLenght / 100) * _BagQty * Utility.SafeConvertToDouble (textLoopNo.Text));
                        //            LoopTotalMtr = Math.Round(LoopTotalMtr, 4);

                        //        }
                        //        if (checkBoxliner.Checked)
                        //        {
                        //            LinerWtFormula();
                        //            LinerWt = LinerWt / 10000000;
                        //            LinerWt = Math.Round(LinerWt, 4);
                        //            LinerTotalMtr = ((LinerCutLenght / 100) * _BagQty)
                        //                  + .1 * ((LinerCutLenght / 100) * _BagQty);
                        //            LinerTotalMtr = Math.Round(LinerTotalMtr, 4);
                        //        }

                        //        ThreadWtFormula();

                        //        if (checkSpoutRope.Checked)
                        //        {
                        //            TopSpoutRopeWtFormula();
                        //            TopSpoutRopeWt = TopSpoutRopeWt / 10000000;
                        //            TopSpoutRopeWt = Math.Round(TopSpoutRopeWt, 4);
                        //            TopSpoutRopeTotalMtr = ((TopSpoutRopeCutLenght / 100) * _BagQty)
                        //                  + .1 * ((TopSpoutRopeCutLenght / 100) * _BagQty);
                        //            TopSpoutRopeTotalMtr = Math.Round(TopSpoutRopeTotalMtr, 4);
                        //        }

                        //        if (checkTopTie.Checked)
                        //        {
                        //            TopTieFormula();

                        //            TopTieWt = TopTieWt / 100000;
                        //            TopTieWt = Math.Round(TopTieWt, 4);
                        //            TopTieTotalMtr = ((TopTieCutLenght / 100) * _BagQty)
                        //                  + .1 * ((TopTieCutLenght / 100) * _BagQty);
                        //            TopTieTotalMtr = Math.Round(TopTieTotalMtr, 4);

                        //        }

                        //        if (checkBottomTie.Checked)
                        //        {
                        //            BottomTieFormula();

                        //            BottomTieWt = BottomTieWt / 100000;
                        //            BottomTieWt = Math.Round(BottomTieWt, 4);
                        //            BottomTieTotalMtr = ((BottomTieCutLenght / 100) * _BagQty)
                        //                  + .1 * ((BottomTieCutLenght / 100) * _BagQty);
                        //            BottomTieTotalMtr = Math.Round(BottomTieTotalMtr, 4);
                        //        }

                        //        if (checkBottomSpoutRope.Checked)
                        //        {
                        //            BottomSpoutRopeWtFormula();

                        //            BottomSpoutRopeWt = BottomSpoutRopeWt / 100000;
                        //            BottomSpoutRopeWt = Math.Round(BottomSpoutRopeWt, 4);
                        //            BottomSpoutRopeTotalMtr = ((BottomSpoutRopeCutLenght / 100) * _BagQty)
                        //                  + .1 * ((BottomSpoutRopeCutLenght / 100) * _BagQty);
                        //            BottomSpoutRopeTotalMtr = Math.Round(BottomSpoutRopeTotalMtr, 4);
                        //        }

                        //        if (checkTopFlapHook.Checked)
                        //        {
                        //            TopHookFormula();

                        //            TopHookWt = TopHookWt / 100000;
                        //            TopHookWt = Math.Round(TopHookWt, 4);
                        //            TopHookTotalMtr = ((TopHookCutLenght / 100) * _BagQty)
                        //                  + .1 * ((TopHookCutLenght / 100) * _BagQty);
                        //            TopHookTotalMtr = Math.Round(TopHookTotalMtr, 4);

                        //        }

                        //        if (checkBottomFlapHook.Checked)
                        //        {
                        //            BottomHookFormula();

                        //            BottomHookWt = BottomHookWt / 10000000;
                        //            BottomHookWt = Math.Round(BottomHookWt, 4);
                        //            BottomHookTotalMtr = ((BottomHookCutLenght / 100) * _BagQty)
                        //                  + .1 * ((BottomHookCutLenght / 100) * _BagQty);
                        //            BottomHookTotalMtr = Math.Round(BottomHookTotalMtr, 4);
                        //        }

                        //        if (checktopflap.Checked)
                        //        {
                        //            TopFlapWtFormula();

                        //            TopFlapWt = TopFlapWt / 10000000;
                        //            TopFlapWt = Math.Round(TopFlapWt, 4);
                        //            TopFlapTotalMtr = ((TopFlapCutLenght / 100) * _BagQty)
                        //                  + .1 * ((TopFlapCutLenght / 100) * _BagQty);
                        //            TopFlapTotalMtr = Math.Round(TopFlapTotalMtr, 4);
                        //        }

                        //        if (checkBottomflap.Checked)
                        //        {
                        //            BottomFlapWtFormula();

                        //            BottomFlapWt = BottomFlapWt / 10000000;
                        //            BottomFlapWt = Math.Round(BottomFlapWt, 4);
                        //            BottomFlapTotalMtr = ((BottomFlapCutLenght / 100) * _BagQty)
                        //                  + .1 * ((BottomFlapCutLenght / 100) * _BagQty);
                        //            BottomFlapTotalMtr = Math.Round(BottomFlapTotalMtr, 4);
                        //        }
                        //        if (checkBottomRope.Checked)
                        //        {
                        //            BottomRopeWtFormula();
                        //            BottomRopeWt = BottomRopeWt / 100000;
                        //            BottomRopeWt = Math.Round(BottomRopeWt, 4);
                        //            BottomRopeTotalMtr = ((BottomRopeCutLenght / 100) * _BagQty)
                        //                  + .1 * ((BottomRopeCutLenght / 100) * _BagQty);
                        //            BottomRopeTotalMtr = Math.Round(BottomRopeTotalMtr, 4);
                        //        }
                        //        if (checkTopRope.Checked)
                        //        {
                        //            TopRopeWtFormula();
                        //            TopRopeWt = TopRopeWt / 100000;
                        //            TopRopeWt = Math.Round(TopRopeWt, 4);
                        //            TopRopeTotalMtr = ((TopRopeCutLenght / 100) * _BagQty)
                        //                  + .1 * ((TopRopeCutLenght / 100) * _BagQty);
                        //            TopRopeTotalMtr = Math.Round(TopRopeTotalMtr, 4);
                        //        }
                        //        if (checkFillerCord.Checked)
                        //        {
                        //            FillerCordWtFormula();

                        //            FillerCordWt = FillerCordWt / 100000;
                        //            FillerCordWt = Math.Round(FillerCordWt, 4);
                        //        }
                        //        if (comboLoopConst.SelectedIndex == 3 && (_BodyIndex1 == 0 || _BodyIndex1 == 1
                        //             || _BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4))
                        //        {
                        //            FullLoopWt = FullLoopWt / 100000;
                        //            FullLoopWt = Math.Round(FullLoopWt, 4);

                        //            FullLoopTotalMtr = ((FullLoopCutLenght / 100) * _BagQty)
                        //                    + .1 * ((BottomFlapCutLenght / 100) * _BagQty);
                        //            FullLoopTotalMtr = Math.Round(FullLoopTotalMtr, 4);
                        //        }
                        //        if (checkBoxTopBand.Checked)
                        //            TopBandWtFormula();
                        //        if (checkLoopCover.Checked)
                        //            LoopCoverWtFormula();
                        //        if (_BodyIndex1 == 2) //Buffle
                        //        {
                        //            BuffleWtFormula();
                        //            BuffleWt = BuffleWt / 10000000;
                        //            BuffleWt = Math.Round(BuffleWt, 4);

                        //            BuffleTotalMtr = (((BuffleCutLenght / 100) * _BagQty)
                        //                    + (.1 * ((BuffleCutLenght / 100) * _BagQty))) * 4;
                        //            BuffleTotalMtr = Math.Round(BuffleTotalMtr, 4);
                        //        }

                        //        if (checkInnerBox.Checked)
                        //            InnerBoxWtFormula();
                        //        if (checkStevdore.Checked)
                        //            StevedoreWtFormula();
                        //        if (checkLoopProtector.Checked)
                        //            LoopProtectorWtFormula();

                        //        if (checkInnerSkin.Checked)
                        //        {
                        //            InnerSkinWtFormula();
                        //            InnerSkinWt = InnerSkinWt / 10000000;
                        //            InnerSkinWt = Math.Round(InnerSkinWt, 4);
                        //            if ((_BodyIndex1 == 2 && (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 1))
                        //                     || _BodyIndex1 == 3)
                        //            {
                        //                if (textBodyL.Text == textBodyW.Text)
                        //                {
                        //                    InnerSkinTotalMtr = ((InnerSkinCutLenght / 100) * _BagQty * 4)
                        //                          + .1 * ((InnerSkinCutLenght / 100) * _BagQty * 4);
                        //                    InnerSkinTotalMtr = Math.Round(InnerSkinTotalMtr, 4);
                        //                }
                        //                else
                        //                {
                        //                    InnerSkinTotalMtr = ((InnerSkinCutLenght / 100) * _BagQty * 2)
                        //                          + .1 * ((InnerSkinCutLenght / 100) * _BagQty * 2);
                        //                    InnerSkinTotalMtr = Math.Round(InnerSkinTotalMtr, 4);
                        //                }

                        //            }
                        //            else
                        //            {
                        //                InnerSkinTotalMtr = ((InnerSkinCutLenght / 100) * _BagQty)
                        //                      + .1 * ((InnerSkinCutLenght / 100) * _BagQty);
                        //                InnerSkinTotalMtr = Math.Round(InnerSkinTotalMtr, 4);
                        //            }
                        //        }

                        //        if (checkInnerTop.Checked)
                        //        {
                        //            InnerTopWtFormula();
                        //            InnerTopWt = InnerTopWt / 10000000;
                        //            InnerTopWt = Math.Round(InnerTopWt, 4);
                        //            InnerTopTotalMtr = ((InnerTopCutLenght / 100) * _BagQty)
                        //                  + .1 * ((InnerTopCutLenght / 100) * _BagQty);
                        //            InnerTopTotalMtr = Math.Round(InnerTopTotalMtr, 4);
                        //        }

                        //        if (checkInnerBottom.Checked)
                        //        {
                        //            InnerBottomWtFormula();

                        //            InnerBottomWt = InnerBottomWt / 10000000;
                        //            InnerBottomWt = Math.Round(InnerBottomWt, 4);
                        //            InnerBottomTotalMtr = ((InnerBottomCutLenght / 100) * _BagQty)
                        //                  + .1 * ((InnerBottomCutLenght / 100) * _BagQty);
                        //            InnerBottomTotalMtr = Math.Round(InnerBottomTotalMtr, 4);

                        //        }
                        //        if (checkAncerieLoop.Checked)
                        //            AncerieWtFormula();

                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        MessageBox.Show(ex.ToString());
                        //        IsError = true;
                        //    }

                        //    if (!IsError)
                        //    {
                        //        string SrNo = "";
                        //        Database.myreader = Database.GetExecuteReaderCommand("select srno from BOM1 WITH(nolock) where FilePONo ='"
                        //          + textFilePONo.Text + "'");
                        //        if (Database.myreader.Read())
                        //            SrNo = Database.myreader[0].ToString();
                        //        Database.myreader.Close();

                        //        //Database.GetExecuteNonQueryCommand("delete from BOM1 where srno = 'temp'");
                        //        //Database.GetExecuteNonQueryCommand("delete from BOM2 where srno = 'temp'");

                        //        Database.GetExecuteNonQueryCommand("delete from BOM1 where FilePONo = '" + textFilePONo.Text + "'");
                        //        Database.GetExecuteNonQueryCommand("delete from BOM2 where PONo = '" + textFilePONo.Text + "'");
                        //        try
                        //        {
                        //            /////////////////////////////
                        //            int count = 0, x = 0;
                        //            SqlDataAdapter myadpter1 = Database.GetAdapterCommand("Select  * from BOM2 WITH(nolock) where 1=2");
                        //            DataSet dataset2 = new DataSet();
                        //            myadpter1.Fill(dataset2);
                        //            SqlCommandBuilder cmd = new SqlCommandBuilder();


                        //            if (checkBoxTunnel.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Tunnel";

                        //                if (checkTunnelLam.Checked)
                        //                    drs[1] = comboTunnelGSM.Text + " + " + comboTunnelLamiGSM.Text;
                        //                else
                        //                    drs[1] = comboTunnelGSM.Text;

                        //                if (checkTunnelLam.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";
                        //                drs[4] = TunnelFabricSize;
                        //                drs[5] = TunnelCutLenght;
                        //                drs[6] = TunnelTotalMtr;
                        //                drs[7] = TunnelWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += TunnelWt;
                        //                count = 1;
                        //            }

                        //            if (_BodyIndex1 == 2) //Buffle
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Buffle";
                        //                drs[1] = textBuffleGSM.Text + "+" + textSingleCoatedGSM.Text + "+" + textDoubleCoatedGSM.Text;
                        //                drs[2] = "Laminated";
                        //                drs[4] = BuffleFabricSize;
                        //                drs[5] = BuffleCutLenght;
                        //                drs[6] = BuffleTotalMtr;
                        //                drs[7] = BuffleWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += BuffleWt;
                        //            }

                        //            if (checkAncerieLoop.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Ancillay Loop";
                        //                drs[1] = comboAncerieGrm.Text;
                        //                drs[2] = "";
                        //                drs[3] = comboAncerieColor.Text;
                        //                drs[4] = AncerieFabricSize;
                        //                drs[5] = AncerieCutLenght;
                        //                drs[6] = AncerieTotalMtr;
                        //                drs[7] = AncerieWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += AncerieWt;
                        //            }

                        //            if (comboFillerCord.SelectedIndex == 3)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Felt";
                        //                drs[7] = FeltWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += FeltWt;
                        //            }

                        //            if (checktopflap.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Top Flap";
                        //                if (checkTopFlapLami.Checked)
                        //                    drs[1] = comboBoxtopflapgsm.Text + " + " + comboTopflapLamiGsm.Text;
                        //                else
                        //                    drs[1] = comboBoxtopflapgsm.Text;

                        //                if (checkTopFlapLami.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";

                        //                drs[3] = comboTopFlapColor.Text;
                        //                drs[4] = TopFlapFabricSize.ToString();
                        //                drs[5] = TopFlapCutLenght.ToString();
                        //                drs[6] = TopFlapTotalMtr.ToString();
                        //                drs[7] = TopFlapWt.ToString();
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += TopFlapWt;
                        //                count = 1;
                        //            }

                        //            if (checkBottomflap.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Bottom Flap";

                        //                if (checkBottomFlapLami.Checked)
                        //                    drs[1] = comboBottomflapGSM.Text + " + " + comboBottomflapLamiGSM.Text;
                        //                else
                        //                    drs[1] = comboBottomflapGSM.Text;

                        //                if (checkBottomFlapLami.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";

                        //                drs[3] = comboBottomFlapColor.Text;

                        //                drs[4] = BottomFlapFabricSize.ToString();
                        //                drs[5] = BottomFlapCutLenght.ToString();
                        //                drs[6] = BottomFlapTotalMtr.ToString();
                        //                drs[7] = BottomFlapWt.ToString();
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += BottomFlapWt;
                        //                count = 1;

                        //            }

                        //            if (checkTopRope.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Top Rope";
                        //                drs[1] = comboTopRopeGrms.Text;
                        //                drs[3] = comboTopRopeColor.Text;
                        //                drs[4] = TopRopeFabricSize.ToString();
                        //                drs[5] = TopCutLenght.ToString();
                        //                drs[6] = TopRopeTotalMtr.ToString();
                        //                drs[7] = TopRopeWt.ToString();
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += TopRopeWt;
                        //                count = 1;

                        //            }


                        //            if (checkBottomRope.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Bottom Rope";
                        //                drs[1] = comboBottomRopeGrms.Text;
                        //                drs[3] = comboBottomRopeColor.Text;
                        //                drs[4] = BottomRopeFabricSize.ToString();
                        //                drs[5] = BottomRopeCutLenght.ToString();
                        //                drs[6] = BottomRopeTotalMtr.ToString();
                        //                drs[7] = BottomRopeWt.ToString();
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += BottomRopeWt;
                        //                count = 1;

                        //            }

                        //            if (checkSpoutRope.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "TopSpout Rope";
                        //                drs[1] = comboTopSpoutRopeGrm.Text;
                        //                drs[3] = comboTopSpoutRopeColor.Text;
                        //                drs[4] = TopSpoutRopeFabricSize.ToString();
                        //                drs[5] = TopSpoutRopeCutLenght.ToString();
                        //                drs[6] = TopSpoutRopeTotalMtr.ToString();
                        //                drs[7] = TopSpoutRopeWt.ToString();
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += TopSpoutRopeWt;
                        //                count = 1;

                        //            }

                        //            if (checkBottomSpoutRope.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "BottomSpout Rope";
                        //                drs[1] = comboBottomSpoutRopeGrm.Text;
                        //                drs[3] = comboBottomRopeColor.Text;
                        //                drs[4] = BottomSpoutRopeFabricSize.ToString();
                        //                drs[5] = BottomSpoutRopeCutLenght.ToString();
                        //                drs[6] = BottomSpoutRopeTotalMtr.ToString();
                        //                drs[7] = BottomSpoutRopeWt.ToString();
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += BottomSpoutRopeWt;
                        //                count = 1;

                        //            }



                        //            if (checkTopFlapHook.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Top Hook";
                        //                drs[1] = comboTopflapHookGrm.Text;
                        //                drs[3] = comboTopHookColor.Text;
                        //                drs[4] = TopHookFabricSize;
                        //                drs[5] = TopHookCutLenght;
                        //                drs[6] = TopHookTotalMtr;
                        //                drs[7] = TopHookWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += TopHookWt;
                        //                count = 1;

                        //            }

                        //            if (checkBottomFlapHook.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Bottom Hook";
                        //                drs[1] = comboBottomFlapHookGrm.Text;
                        //                drs[3] = comboBottomHookColor.Text;

                        //                drs[4] = BottomHookFabricSize;
                        //                drs[5] = BottomHookCutLenght;
                        //                drs[6] = BottomHookTotalMtr;
                        //                drs[7] = BottomHookWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += BottomHookWt;
                        //                count = 1;

                        //            }

                        //            if (checkTopTie.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Top Tie";
                        //                drs[1] = comboTopTieGrms.Text;
                        //                drs[3] = comboTopTieColor.Text;
                        //                drs[4] = TopTieFabricSize;
                        //                drs[5] = TopTieCutLenght;
                        //                drs[6] = TopTieTotalMtr;
                        //                drs[7] = TopTieWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += TopTieWt;
                        //                count = 1;

                        //            }

                        //            if (checkBottomTie.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Bottom Tie";
                        //                drs[1] = comboBottomTieGrm.Text;
                        //                drs[3] = comboBottomTieColor.Text;
                        //                drs[4] = BottomTieFabricSize;
                        //                drs[5] = BottomTieCutLenght;
                        //                drs[6] = BottomTieTotalMtr;
                        //                drs[7] = BottomTieWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += BottomTieWt;
                        //                count = 1;

                        //            }

                        //            if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5) // Duffle
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Duffle/Skrit";
                        //                if (checkBoxTopLam.Checked)
                        //                    drs[1] = comboBoxTopGSM.Text + " + " + comboTopLamiGSM.Text;
                        //                else
                        //                    drs[1] = comboBoxTopGSM.Text;
                        //                if (checkBoxTopLam.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";
                        //                drs[4] = DuffleFabricSize.ToString();
                        //                drs[5] = DuffleCutLenght.ToString();
                        //                drs[6] = DuffleTotalMtr.ToString();
                        //                drs[7] = DuffleWt.ToString();
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += DuffleWt;
                        //                count = 1;

                        //            }

                        //            if (comboBoxbottomtype.SelectedIndex == 8) //Bottom Duffle
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Bottom Duffle/Skrit";
                        //                if (checkBoxbottomlam.Checked)
                        //                    drs[1] = comboBoxbottomgsm.Text + " + " + comboBottomLamiGSM.Text;
                        //                else
                        //                    drs[1] = comboBoxbottomgsm.Text;
                        //                if (checkBoxbottomlam.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";
                        //                drs[4] = BottomDuffleFabricSize.ToString();
                        //                drs[5] = BottomDuffleCutLenght.ToString();
                        //                drs[6] = BottomDuffleTotalMtr.ToString();
                        //                drs[7] = BottomDuffleWt.ToString();
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += BottomDuffleWt;
                        //                count = 1;
                        //            }

                        //            if (comboLoopConst.SelectedIndex == 3 && (_BodyIndex1 == 0 || _BodyIndex1 == 1
                        //          || _BodyIndex1 == 2 || _BodyIndex1 == 3 || _BodyIndex1 == 4))
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Full Loop";

                        //                drs[4] = FullLoopFabricSize;
                        //                drs[5] = FullLoopCutLenght;
                        //                drs[6] = FullLoopTotalMtr;
                        //                drs[7] = FullLoopWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);

                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += FullLoopWt;
                        //                count = 1;

                        //            }

                        //            if (checkBoxTopBand.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Top Band";
                        //                drs[1] = combotopbandgrm.Text;
                        //                drs[3] = comboTopBandColor.Text;
                        //                drs[4] = TopBandFabricSize;
                        //                drs[5] = TopBandCutLenght;
                        //                drs[6] = TopBandTotalMtr;
                        //                drs[7] = TopBandWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;
                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += TopBandWt;
                        //                count = 1;

                        //            }

                        //            if (checkLoopCover.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Loop Cover";


                        //                if (checkLoopCoverLami.Checked)
                        //                    drs[1] = comboLoopCoverGSM.Text + " + " + comboLoopCoverLamiGSM.Text;
                        //                else
                        //                    drs[1] = comboLoopCoverGSM.Text;

                        //                if (checkLoopCoverLami.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";

                        //                drs[3] = comboLoopCoverColor.Text;
                        //                drs[4] = comboLoopCoverSize.Text;
                        //                drs[5] = comboLoopCoverCutSize.Text;
                        //                drs[6] = LoopCOverTotalMtr;
                        //                drs[7] = LoopCoverWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += LoopCoverWt;
                        //                count = 1;

                        //            }


                        //            if (checkInnerBox.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "InnerBox";

                        //                if (checkInnerBoxLami.Checked)
                        //                    drs[1] = comboInnerBoxGSM.Text + " + " + comboInnerBoxLamiGSM.Text;
                        //                else
                        //                    drs[1] = comboInnerBoxGSM.Text;

                        //                if (checkInnerBoxLami.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";

                        //                drs[3] = comboInnerBoxColor.Text;
                        //                drs[4] = InnerBoxFabricSize;
                        //                drs[5] = InnerBoxCutLenght;
                        //                drs[6] = InnerBoxTotalMtr;
                        //                drs[7] = InnerBoxWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += InnerBoxWt;
                        //                count = 1;

                        //            }

                        //            if (checkLoopProtector.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Loop Protector";

                        //                if (checkLoopProcLami.Checked)
                        //                    drs[1] = comboLoopProtectorGSM.Text + " + " + comboLoopProctectorLamiGSM.Text;
                        //                else
                        //                    drs[1] = comboLoopProtectorGSM.Text;

                        //                if (checkLoopProcLami.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";

                        //                drs[3] = comboLoopProctectorColor.Text;
                        //                drs[4] = LoopProtectorFabricSize;
                        //                drs[5] = LoopProtectorCutLenght;
                        //                drs[6] = LoopProtectorTotalMtr;
                        //                drs[7] = LoopProtectorWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += LoopProtectorWt;
                        //                count = 1;

                        //            }

                        //            if (checkStevdore.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "Stevedore";
                        //                drs[1] = comboStGrm.Text;
                        //                drs[3] = comboSteveDoreColor.Text;
                        //                drs[4] = StevedoreFabricSize;
                        //                drs[5] = StevedoreCutLenght;
                        //                drs[6] = StevedoreTotalMtr;
                        //                drs[7] = StevedoreWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;
                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);
                        //                TotalKg += StevedoreWt;
                        //                count = 1;

                        //            }

                        //            if (checkInnerSkin.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "InnerSkin";

                        //                if (checkInnerSkinLami.Checked)
                        //                    drs[1] = comboInnerSkinGSM.Text + " + " + comboInnerSkinLamiGSM.Text;
                        //                else
                        //                    drs[1] = comboInnerSkinGSM.Text;

                        //                if (checkInnerSkinLami.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";
                        //                drs[3] = comboInnerSkinColor.Text;
                        //                drs[4] = InnerSkinFabricSize;
                        //                drs[5] = InnerSkinCutLenght;
                        //                drs[6] = InnerSkinTotalMtr;
                        //                drs[7] = InnerSkinWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += InnerSkinWt;
                        //                count = 1;

                        //            }

                        //            if (checkInnerTop.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "InnerTop";

                        //                if (checkInnerTopLami.Checked)
                        //                    drs[1] = comboInnerTopGSM.Text + " + " + comboInnerTopLamiGSM.Text;
                        //                else
                        //                    drs[1] = comboInnerTopGSM.Text;

                        //                if (checkInnerTopLami.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";

                        //                drs[3] = comboInnerTopColor.Text;
                        //                drs[4] = InnerTopFabricSize;
                        //                drs[5] = InnerTopCutLenght;
                        //                drs[6] = InnerTopTotalMtr;
                        //                drs[7] = InnerTopWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += InnerTopWt;
                        //                count = 1;

                        //            }

                        //            if (checkInnerBottom.Checked)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[0] = "InnerBottom";

                        //                if (checkInnerBottomLami.Checked)
                        //                    drs[1] = comboInnerBottomGSM.Text + " + " + comboInnerBottomLamiGSM.Text;
                        //                else
                        //                    drs[1] = comboInnerBottomGSM.Text;

                        //                if (checkInnerBottomLami.Checked)
                        //                    drs[2] = "Laminated";
                        //                else
                        //                    drs[2] = "UnLaminated";

                        //                drs[3] = comboInnerBottomColor.Text;
                        //                drs[4] = InnerBottomFabricSize;
                        //                drs[5] = InnerBottomCutLenght;
                        //                drs[6] = InnerBottomTotalMtr;
                        //                drs[7] = InnerBottomWt;
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                x = myadpter1.Update(dataset2.Tables[0]);

                        //                TotalKg += InnerBottomWt;
                        //                count = 1;

                        //            }

                        //            if (count == 0)
                        //            {
                        //                DataRow drs = dataset2.Tables[0].NewRow();
                        //                drs[8] = textFilePONo.Text;
                        //                drs[9] = SrNo;

                        //                dataset2.Tables[0].Rows.Add(drs);
                        //                cmd = new SqlCommandBuilder(myadpter1);
                        //                myadpter1.Update(dataset2.Tables[0]);
                        //            }


                        //            ///////////////////////////
                        //            SqlDataAdapter myadapter = Database.GetAdapterCommand("select * from BOM1 WITH(nolock) where 1=2");
                        //            DataSet dataset1 = new DataSet();
                        //            myadapter.Fill(dataset1);

                        //            DataRow dr = dataset1.Tables[0].NewRow();
                        //            dr[0] = EnqdateTime.Text;
                        //            dr[1] = comboPartyName.Text;
                        //            dr[2] = comboPrintType.Text;
                        //            dr[3] = textFilePONo.Text;
                        //            if (checkBoxTunnel.Checked)
                        //                dr[4] = comboBody1.Text + "/Tunnel Bag ";
                        //            else
                        //                dr[4] = comboBody1.Text + "/" + comboBody2.Text + "/" + comboBody3.Text;
                        //            dr[5] = textBodyL.Text;
                        //            dr[6] = textBodyW.Text;
                        //            dr[7] = textBodyH.Text;
                        //            dr[8] = comboType.Text;
                        //            dr[9] = textSWL.Text;
                        //            if (checkBoxdocpouch.Checked)
                        //                dr[10] = comboDocType.Text + "/" + comboDocType1.Text + "/" + comboDocType2.Text;
                        //            else
                        //                dr[10] = "N/A";

                        //            if (checkBoxLoop.Checked)
                        //            {
                        //                dr[11] = textShortLeg.Text;
                        //                dr[12] = comboLoopL.Text;
                        //                dr[13] = comboLoopW.Text;
                        //            }
                        //            dr[14] = comboBodyUnit.Text;
                        //            dr[15] = comboSF.Text;
                        //            if (checkBoxTop.Checked)
                        //            {
                        //                if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5)
                        //                    dr[17] = comboTopType.Text;
                        //                else
                        //                {
                        //                    dr[16] = comboTopType.Text;
                        //                    dr[17] = comboSpoutType.Text;
                        //                }
                        //            }
                        //            else
                        //                dr[17] = "Open";
                        //            //if (checkBoxTop.Checked)
                        //            //    dr[17] = comboSpoutType.Text;
                        //            //else
                        //            //    dr[17] = "Open";

                        //            if (checkBoxliner.Checked)
                        //            {
                        //                dr[18] = comboBoxlinerheight.Text;
                        //                dr[19] = comboBoxlinerwidth.Text;
                        //                dr[20] = comboBoxlinermicron.Text;
                        //                dr[21] = comboBoxlinertype.Text;
                        //                dr[22] = comboBoxlinertype1.Text;
                        //            }



                        //            dr[23] = textQty.Text;

                        //            if (checkbottom.Checked)
                        //            {
                        //                dr[25] = comboBoxbottomdia.Text;
                        //                dr[26] = comboBoxbottomheight.Text;
                        //                dr[29] = comboBoxbottomtype.Text + "/" + comboBoxbottomsubtype.Text;
                        //            }
                        //            else
                        //                dr[29] = "Flat";
                        //            dr[28] = comboBodyColor.Text;


                        //            if (checkBoxTop.Checked)
                        //            {
                        //                dr[30] = comboSpoutDia.Text;
                        //                dr[31] = comboSpoutHeight.Text;
                        //            }
                        //            // Body Formula
                        //            if (checkBoxLam.Checked)
                        //                dr[33] = comboBodyGSM.Text + " + " + comboBodyLamiGSM.Text;
                        //            else
                        //                dr[33] = comboBodyGSM.Text;
                        //            if (checkBoxLam.Checked)
                        //                dr[34] = "Laminated";
                        //            else
                        //                dr[34] = "UnLaminated";
                        //            dr[35] = BodyFabricSize.ToString();
                        //            dr[36] = BodyCutLenght.ToString();
                        //            dr[37] = BodyTotalMtr.ToString();
                        //            dr[38] = BodyWt.ToString();

                        //            TotalKg += BodyWt;
                        //            // Side Formula
                        //            if (checkSide.Checked)
                        //            {
                        //                if (checkSideLami.Checked)
                        //                    dr[40] = comboSideGSM.Text + " + " + comboSideLamiGSM.Text;
                        //                else
                        //                    dr[40] = comboSideGSM.Text;

                        //                if (checkSideLami.Checked)
                        //                    dr[41] = "Laminated";
                        //                else
                        //                    dr[41] = "UnLaminated";
                        //                dr[42] = SideFabricSize.ToString();
                        //                dr[43] = SideCutLenght.ToString();
                        //                dr[44] = SideTotalMtr.ToString();
                        //                dr[45] = SideWt.ToString();
                        //                TotalKg += SideWt;
                        //            }

                        //            // Top Formula
                        //            if (checkBoxTop.Checked && comboTopType.SelectedIndex != 0 && comboTopType.SelectedIndex != 2)
                        //            {
                        //                if (checkBoxTopLam.Checked)
                        //                    dr[47] = comboBoxTopGSM.Text + " + " + comboTopLamiGSM.Text;
                        //                else
                        //                    dr[47] = comboBoxTopGSM.Text;
                        //                if (checkBoxTopLam.Checked)
                        //                    dr[48] = "Laminated";
                        //                else
                        //                    dr[48] = "UnLaminated";
                        //                dr[49] = TopFabricSize.ToString();
                        //                dr[50] = TopCutLenght.ToString();
                        //                dr[51] = TopTotalMtr.ToString();
                        //                dr[52] = TopWt.ToString();
                        //                dr[53] = comboTopColor.Text;
                        //                TotalKg += TopWt;
                        //            }

                        //            // Bottom Formula
                        //            if (checkbottom.Checked && _BodyIndex1 != 0)
                        //            {
                        //                if (checkBoxbottomlam.Checked)
                        //                    dr[54] = comboBoxbottomgsm.Text + " + " + comboBottomLamiGSM.Text;
                        //                else
                        //                    dr[54] = comboBoxbottomgsm.Text;
                        //                if (checkBoxbottomlam.Checked)
                        //                    dr[55] = "Laminated";
                        //                else
                        //                    dr[55] = "UnLaminated";
                        //                dr[56] = BaseFabricSize.ToString();
                        //                dr[57] = BaseCutLenght.ToString();
                        //                dr[58] = BaseTotalMtr.ToString();
                        //                dr[59] = BaseWt.ToString();
                        //                dr[60] = comboBottomColor.Text;
                        //                TotalKg += BaseWt;
                        //            }

                        //            // Top Spout/ FS Spout
                        //            if (comboTopType.SelectedIndex == 1 || comboTopType.SelectedIndex == 3 || comboTopType.SelectedIndex == 4) //Top Type = Top Spout
                        //            {
                        //                if (checkBoxSpoutLam.Checked)
                        //                    dr[61] = comboSpoutGSM.Text + " + " + comboSpoutLamiGSM.Text;
                        //                else
                        //                    dr[61] = comboSpoutGSM.Text;
                        //                if (checkBoxSpoutLam.Checked)
                        //                    dr[62] = "Laminated";
                        //                else
                        //                    dr[62] = "UnLaminated";
                        //                dr[63] = FSFabricSize.ToString();
                        //                dr[64] = FSCutLenght.ToString();
                        //                dr[65] = FSTotalMtr.ToString();
                        //                dr[66] = FSWt.ToString();
                        //                dr[67] = comboSpoutColor.Text;
                        //                TotalKg += FSWt;
                        //            }

                        //            // FSTie
                        //            if (checkSpoutTie.Checked)
                        //            {
                        //                dr[68] = comboSpoutTieGrm.Text;
                        //                dr[69] = "";
                        //                dr[70] = FSTieFabricSize;
                        //                dr[71] = FSTieCutLenght;
                        //                dr[72] = FSTieTotalMtr;
                        //                dr[73] = FSTieWt;
                        //                dr[74] = comboTopSpoutTieColor.Text;
                        //                TotalKg += FSTieWt;
                        //            }

                        //            //// Bottom Spout
                        //            if (checkbottom.Checked && (comboBoxbottomsubtype.SelectedIndex == 0 || comboBoxbottomsubtype.SelectedIndex == 1
                        //                || comboBoxbottomsubtype.SelectedIndex == 2))
                        //            {
                        //                if (checkBoxbottomlam1.Checked)
                        //                    dr[75] = comboBoxbottomgsm1.Text + " + " + comboBoxBottomSubTypeLamiGSM.Text;
                        //                else
                        //                    dr[75] = comboBoxbottomgsm1.Text;
                        //                if (checkBoxbottomlam1.Checked)
                        //                    dr[76] = "Laminated";
                        //                else
                        //                    dr[76] = "UnLaminated";
                        //                dr[77] = DSFabricSize.ToString();
                        //                dr[78] = DSCutLenght.ToString();
                        //                dr[79] = DSTotalMtr.ToString();
                        //                dr[80] = DSWt.ToString();
                        //                dr[81] = comboBottomSpoutColor.Text;
                        //                TotalKg += DSWt;
                        //            }


                        //            // DSTie
                        //            if (checkBottomSpoutTie.Checked)
                        //            {
                        //                dr[82] = comboBottomSpoutTieGrm.Text;
                        //                dr[83] = "";
                        //                dr[84] = DSTieFabricSize;
                        //                dr[85] = DSTieCutLenght;
                        //                dr[86] = DSTieTotalMtr;
                        //                dr[87] = DSTieWt;
                        //                dr[88] = comboBottomSpoutTieColor.Text;
                        //                TotalKg += DSTieWt;
                        //            }

                        //            if (checkBoxLoop.Checked)
                        //            {
                        //                dr[89] = comboLoopGrm.Text;
                        //                dr[90] = comboLoopColor.Text;
                        //                dr[91] = LoopFabricSize.ToString();
                        //                dr[92] = LoopCutLenght.ToString();
                        //                dr[93] = LoopTotalMtr.ToString();
                        //                dr[94] = LoopWt.ToString();
                        //                dr[95] = textLoopNo.Text;
                        //                //dr[96] = textLoopNo.Text;
                        //                TotalKg += LoopWt;
                        //            }

                        //            if (checkBoxliner.Checked)
                        //            {
                        //                dr[96] = comboBoxlinermicron.Text;
                        //                dr[97] = comboBoxlinertype.Text;
                        //                dr[98] = LinerFabricSize.ToString();
                        //                dr[99] = LinerCutLenght.ToString();
                        //                dr[100] = LinerTotalMtr.ToString();
                        //                dr[101] = LinerWt.ToString();
                        //                dr[102] = comboLinerColor.Text;
                        //                TotalKg += LinerWt;
                        //            }


                        //            if (checkBoxdocpouch.Checked)
                        //            {
                        //                dr[103] = comboDocMicron.Text;
                        //                dr[104] = DocFabricSize;
                        //                dr[105] = DocCutLenght;
                        //                dr[106] = DocTotalMtr;
                        //                dr[107] = DocWt;
                        //                dr[108] = comboDocColor.Text;
                        //                TotalKg += DocWt;
                        //            }


                        //            if (checkBoxlabel.Checked)
                        //            {
                        //                dr[109] = comboLabelMicron.Text;
                        //                dr[110] = LabelFabricSize;
                        //                dr[111] = LabelCutLenght;
                        //                dr[112] = LabelTotalMtr;
                        //                dr[113] = LabelWt;
                        //                dr[114] = comboLabelColor.Text;
                        //                TotalKg += LabelWt;
                        //            }

                        //            ThreadWt = ThreadWt / 100000;
                        //            ThreadWt = Math.Round(ThreadWt, 4);
                        //            dr[115] = ThreadWt;
                        //            TotalKg += ThreadWt;



                        //            if (checkFillerCord.Checked)
                        //            {
                        //                dr[117] = FillerCordGSM;
                        //                dr[118] = FillerCordWt;
                        //                TotalKg += FillerCordWt;
                        //            }



                        //            dr[119] = FeltWt;
                        //            dr[120] = TotalKg;
                        //            dr[121] = textInstruction.Text;
                        //            dr[122] = textBodyRemarks.Text;
                        //            dr[123] = SrNo;

                        //            if (_BodyIndex1 == 5 || _BodyIndex1 == 6
                        //                       || _BodyIndex1 == 7 || _BodyIndex1 == 8)
                        //            {
                        //                dr[124] = SlitHt;
                        //                dr[125] = textFillHt.Text;
                        //                dr[126] = TotalHt;
                        //            }
                        //            if (checkbottom.Checked)
                        //                dr[127] = textBottomRemarks.Text;
                        //            if (checkSpoutTie.Checked)
                        //                dr[128] = textTopSpoutTieRemarks.Text;
                        //            if (checkBottomSpoutTie.Checked)
                        //                dr[129] = textBottomSpoutTieRemarks.Text;

                        //            if (checkBoxDropLoop.Checked)
                        //            {
                        //                dr[130] = "yes";
                        //                dr[131] = textLoopDropLenght.Text;
                        //            }

                        //            if (textRMPP.Text != "")
                        //                dr[132] = textRMPP.Text;
                        //            else
                        //            {
                        //                dr[132] = "0";
                        //                textRMPP.Text = "0";
                        //            }
                        //            if (textStdConvPP.Text != "")
                        //                dr[133] = textStdConvPP.Text;
                        //            else
                        //            {
                        //                dr[133] = "0";
                        //                textStdConvPP.Text = "0";
                        //            }


                        //            if (textRMPE.Text != "")
                        //                dr[134] = textRMPE.Text;
                        //            else
                        //            {
                        //                dr[134] = "0";
                        //                textRMPE.Text = "0";
                        //            }
                        //            if (textStdConvPE.Text != "")
                        //                dr[135] = textStdConvPE.Text;
                        //            else
                        //            {
                        //                dr[135] = "0";
                        //                textStdConvPE.Text = "0";
                        //            }

                        //            if (checkBoxdocpouch.Checked)
                        //                dr[136] = textDoc.Text;
                        //            dr[137] = textPallets.Text;
                        //            if (textPrintingRate.Text == "")
                        //                textPrintingRate.Text = "0";

                        //            dr[138] = textPrintingRate.Text;
                        //            dr[139] = textBLock.Text;
                        //            dr[140] = textHoseSlider.Text;
                        //            dr[141] = textVelcro.Text;
                        //            dr[142] = textDustProof.Text;
                        //            dr[143] = textFelt.Text;
                        //            if (textFrieght.Text == "")
                        //                textFrieght.Text = "0";

                        //            dr[144] = textFrieght.Text;

                        //            //Group A Rate Wt Calcualtion
                        //            double TotalGroupAWt = 0;
                        //            double TotalGroupARate = 0;
                        //            if (checkBoxliner.Checked)
                        //                TotalGroupAWt = TotalGroupAWt + LinerWt;
                        //            if (checkBoxdocpouch.Checked)
                        //                TotalGroupAWt = TotalGroupAWt + DocWt;
                        //            if (checkFillerCord.Checked)
                        //                TotalGroupAWt = TotalGroupAWt + FillerCordWt;
                        //            if (comboFillerCord.SelectedIndex == 3)
                        //                TotalGroupAWt = TotalGroupAWt + FeltWt;

                        //            TotalGroupARate = Utility.SafeConvertToDouble (textRMPP.Text)
                        //                                + Utility.SafeConvertToDouble (textStdConvPP.Text);

                        //            TotalGroupARate = TotalGroupARate / 1000;

                        //            TotalGroupAWt = TotalKg - TotalGroupAWt;
                        //            // TotalGroupARate = TotalGroupARate * TotalGroupAWt;
                        //            //////////////////////////


                        //            //Group B Rate Wt Calcualtion
                        //            double TotalGroupBWt = 0;
                        //            double TotalGroupBRate = 0;
                        //            if (checkBoxliner.Checked)
                        //            {
                        //                TotalGroupBWt = TotalGroupBWt + LinerWt;

                        //                TotalGroupBRate = Utility.SafeConvertToDouble (textRMPE.Text)
                        //                                    + Utility.SafeConvertToDouble (textStdConvPE.Text);

                        //                TotalGroupBRate = TotalGroupBRate / 1000;

                        //                if (combolinersubtype.SelectedIndex == 1) //Tabbed
                        //                {
                        //                    if (comboBoxlineratpoint.Text == "4")
                        //                        TotalGroupBRate = TotalGroupBRate + .17;
                        //                    else if (comboBoxlineratpoint.Text == "8")
                        //                        TotalGroupBRate = TotalGroupBRate + .34;
                        //                }

                        //                if (combolinersubtype.SelectedIndex == 2) //Glued
                        //                {
                        //                    if (comboBoxlineratpoint.Text == "4")
                        //                        TotalGroupBRate = TotalGroupBRate + .10;
                        //                    else if (comboBoxlineratpoint.Text == "8")
                        //                        TotalGroupBRate = TotalGroupBRate + .28;
                        //                }


                        //                // TotalGroupBRate = TotalGroupBRate * TotalGroupBWt;
                        //            }
                        //            //////////////////////////


                        //            //Group C Rate Wt Calcualtion
                        //            double TotalGroupCWt = 0;
                        //            double TotalGroupCRate = 0;
                        //            if (checkFillerCord.Checked)
                        //            {
                        //                TotalGroupCWt = TotalGroupCWt + FillerCordWt;
                        //                TotalGroupCRate = TotalGroupCRate + (Utility.SafeConvertToDouble (textDustProof.Text)
                        //                     * FillerCordWt);
                        //            }
                        //            if (comboFillerCord.SelectedIndex == 3)
                        //            {
                        //                TotalGroupCWt = TotalGroupCWt + FeltWt;
                        //                TotalGroupCRate = TotalGroupCRate + (Utility.SafeConvertToDouble (textFelt.Text)
                        //                    * FeltWt);
                        //            }
                        //            //////////////////////////


                        //            //Group D Rate Wt Calcualtion (Add Ons)
                        //            double TotalGroupDRate = 0;
                        //            if (checkBoxdocpouch.Checked)
                        //                TotalGroupDRate = TotalGroupDRate + Utility.SafeConvertToDouble (textDoc.Text);
                        //            TotalGroupDRate = TotalGroupDRate + Utility.SafeConvertToDouble (textPallets.Text);
                        //            TotalGroupDRate = TotalGroupDRate + Utility.SafeConvertToDouble (textPrintingRate.Text);
                        //            if (checkBoxblock.Checked)
                        //                TotalGroupDRate = TotalGroupDRate +
                        //                     (Utility.SafeConvertToDouble (textBLock.Text) * Utility.SafeConvertToDouble (textBoxblocknos.Text));
                        //            if (checkTopVelcro.Checked)
                        //                TotalGroupDRate = TotalGroupDRate +
                        //                      (Utility.SafeConvertToDouble (textVelcro.Text) * Utility.SafeConvertToDouble (comboTopVelcro.Text));
                        //            if (checkbottomvelcro.Checked)
                        //                TotalGroupDRate = TotalGroupDRate +
                        //                      (Utility.SafeConvertToDouble (textVelcro.Text) * Utility.SafeConvertToDouble (combobottomvelcro.Text));

                        //            if (checkTopHoseSlider.Checked)
                        //                TotalGroupDRate = TotalGroupDRate +
                        //                      (Utility.SafeConvertToDouble (textHoseSlider.Text) * Utility.SafeConvertToDouble (comboTopHoseSlider.Text));
                        //            if (checkBottomhoseslider.Checked)
                        //                TotalGroupDRate = TotalGroupDRate +
                        //                      (Utility.SafeConvertToDouble (textHoseSlider.Text) * Utility.SafeConvertToDouble (comboBottomhoseslider.Text));

                        //            //////////////////////////

                        //            double TotalGroupERate = 0;
                        //            if ((comboLoopType.SelectedIndex == 1 || comboLoopType.SelectedIndex == 2) && checkBoxLoop.Checked) //MultiFilament,SeatBelt
                        //            {
                        //                if (textLoopStdConv.Text == "")
                        //                    textLoopStdConv.Text = "0";
                        //                TotalGroupERate = LoopWt * Utility.SafeConvertToDouble (textLoopStdConv.Text);
                        //                TotalGroupERate = TotalGroupERate / 1000;
                        //            }

                        //            if (textFrieght.Text == "")
                        //                textFrieght.Text = "0";

                        //            double TotalFrieght = (TotalKg - DocWt) * Utility.SafeConvertToDouble (textFrieght.Text);
                        //            TotalFrieght = TotalFrieght / 1000;


                        //            if (comboCurrency.SelectedIndex == 0)
                        //            {
                        //                TotalGroupARate = TotalGroupARate * Utility.SafeConvertToDouble (textINR.Text);
                        //                TotalGroupBRate = TotalGroupBRate * Utility.SafeConvertToDouble (textINR.Text);
                        //                TotalGroupCRate = TotalGroupCRate * Utility.SafeConvertToDouble (textINR.Text);
                        //                TotalGroupDRate = TotalGroupDRate * Utility.SafeConvertToDouble (textINR.Text);
                        //                TotalGroupERate = TotalGroupERate * Utility.SafeConvertToDouble (textINR.Text);
                        //                TotalFrieght = TotalFrieght * Utility.SafeConvertToDouble (textINR.Text);
                        //            }
                        //            if (comboCurrency.SelectedIndex == 2)
                        //            {
                        //                TotalGroupARate = TotalGroupARate * Utility.SafeConvertToDouble (textGBP.Text);
                        //                TotalGroupBRate = TotalGroupBRate * Utility.SafeConvertToDouble (textGBP.Text);
                        //                TotalGroupCRate = TotalGroupCRate * Utility.SafeConvertToDouble (textGBP.Text);
                        //                TotalGroupDRate = TotalGroupDRate * Utility.SafeConvertToDouble (textGBP.Text);
                        //                TotalGroupERate = TotalGroupERate * Utility.SafeConvertToDouble (textGBP.Text);
                        //                TotalFrieght = TotalFrieght * Utility.SafeConvertToDouble (textGBP.Text);


                        //            }
                        //            if (comboCurrency.SelectedIndex == 3)
                        //            {
                        //                TotalGroupARate = TotalGroupARate * Utility.SafeConvertToDouble (textEURO.Text);
                        //                TotalGroupBRate = TotalGroupBRate * Utility.SafeConvertToDouble (textEURO.Text);
                        //                TotalGroupCRate = TotalGroupCRate * Utility.SafeConvertToDouble (textEURO.Text);
                        //                TotalGroupDRate = TotalGroupDRate * Utility.SafeConvertToDouble (textEURO.Text);
                        //                TotalGroupERate = TotalGroupERate * Utility.SafeConvertToDouble (textEURO.Text);
                        //                TotalFrieght = TotalFrieght * Utility.SafeConvertToDouble (textEURO.Text);

                        //            }



                        //            if (textDiscount.Text == "")
                        //                textDiscount.Text = "0";





                        //            double TotalRate = (TotalGroupARate * TotalGroupAWt)
                        //                               + (TotalGroupBRate * TotalGroupBWt)
                        //                               + (TotalGroupCRate * TotalGroupCWt)
                        //                               + TotalGroupDRate + TotalGroupERate
                        //                                + TotalFrieght - Utility.SafeConvertToDouble (textDiscount.Text);

                        //            dr[145] = TotalGroupAWt;
                        //            dr[146] = TotalGroupARate;
                        //            dr[147] = TotalGroupBWt;
                        //            dr[148] = TotalGroupBRate;
                        //            dr[149] = TotalGroupCWt;
                        //            dr[150] = TotalGroupCRate;
                        //            dr[151] = TotalGroupDRate;
                        //            dr[152] = TotalFrieght;
                        //            dr[153] = TotalRate;



                        //            if (textFile1.Text.Trim().Length != 0)
                        //            {
                        //                string fileName = Path.GetFileName(textFile1.Text.ToString());
                        //                byte[] content = ReadFileToByteArray(fileName);
                        //                dr[154] = 1;
                        //                dr[155] = fileName;
                        //                dr[156] = content;
                        //            }

                        //            if (comboTopType.SelectedIndex == 2 || comboTopType.SelectedIndex == 5) //Duffle
                        //                dr[157] = comboBoxduffleskirtheight.Text;

                        //            dr[158] = comboCurrency.Text;
                        //            dr[159] = textDiscount.Text;
                        //            if (checkOrderConfirmed.Checked)
                        //                dr[160] = "Confirmed";

                        //            dr[161] = TotalGroupERate;
                        //            if (textLoopStdConv.Text == "")
                        //                dr[162] = "0";
                        //            else
                        //                dr[162] = textLoopStdConv.Text;
                        //            dr[163] = comboLoopType.Text;

                        //            if (_BodyIndex1 == 2)//Buffle
                        //                dr[164] = comboBuffleType.Text;

                        //            dr[165] = textTopRemarks.Text;
                        //            dr[166] = textLoopRemarks.Text;
                        //            dr[167] = textLinerRemarks.Text;
                        //            dr[168] = textLabelRemarks.Text;

                        //            dr[169] = textPerson.Text;

                        //            if (comboBoxbottomtype.SelectedIndex == 8) // Bottom Duffle
                        //                dr[170] = textSkirtHeight.Text;
                        //            if (checkBoxdocpouch.Checked)
                        //                dr[171] = textDocNo.Text;

                        //            dr[172] = textpono.Text;


                        //            dataset1.Tables[0].Rows.Add(dr);
                        //            cmd = new SqlCommandBuilder(myadapter);
                        //            x = myadapter.Update(dataset1.Tables[0]);
                        //            if (x > 0)
                        //                MessageBox.Show("Bill of Material " + SrNo + " is Updated succesfully");

                        //            PopulateData("", SrNo);

                        //            IsTemp = false;
                        //        }
                        //        catch (Exception ex)
                        //        {
                        //            MessageBox.Show(ex.ToString());
                        //        }


                        //        Database.Closeconnection();

                        // }
                        // }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                //Database.RollBackTransaction();
                MessageBox.Show(ex.ToString());
            }
        }
        private void comboBoxbottomsubtype_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (comboBoxbottomsubtype.SelectedIndex == 1)
            {
                comboBottomPetalFlapGSM.Text = comboBodyGSM.Text;
                checkbottomPetalFlapGSM.Checked = true;
                combobottomPetalFlapGSMLam.Text = "25";
            }

            //if (!IsupdateMode) -- 18-09-2021
            {
                checkBottomSpoutRope.Checked = false;
                comboBottomSpoutRopeGrm.Text = "";
                comboBottomSpoutRopeSize.Text = "";
                comboBottomSpoutRope.SelectedIndex = 0;
                checkBottomspoutiristie.Checked = false;
                if (comboBoxbottomsubtype.SelectedIndex != 5)
                {
                    if (comboBoxbottomsubtype.SelectedIndex == 1) //Petal Closure
                    {
                        if (_BodyIndex1 == 0)
                            textBodyRemarks.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);

                        else
                            textBottomrem.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                        checkBottomSpoutRope.Checked = true;
                        comboBottomSpoutRopeGrm.Text = "8";
                        comboBottomSpoutRopeSize.Text = "6";
                        comboBottomSpoutRope.SelectedIndex = 0;

                    }
                    else if (comboBoxbottomsubtype.SelectedIndex == 2 && IsupdateMode == false)
                    {
                        checkBottomspoutiristie.Checked = true;
                    }
                    //else
                    //{
                    comboBoxbottomgsm1.Text = "70";
                    comboBoxBottomSubTypeLamiGSM.Text = "25";
                    checkBoxbottomlam1.Checked = true;
                    comboBoxbottomdia.Text = "35";
                    comboBoxbottomheight.Text = "50";
                    // }

                }
                else
                {
                    comboBoxbottomgsm1.Text = "0";
                    comboBoxBottomSubTypeLamiGSM.Text = "0";
                    checkBoxbottomlam1.Checked = false;
                    comboBoxbottomdia.Text = "0";
                    comboBoxbottomheight.Text = "0";
                }
            }
        }

        //private void btnBrowse1_Click(object sender, EventArgs e)
        //{
        //    openFileDialog1.Filter = "All Files (*.*)|*.*";
        //    openFileDialog1.Title = "Select File";
        //    if (DialogResult.Cancel == openFileDialog1.ShowDialog())
        //    {
        //        return;
        //    }
        //    textFile1.Text = openFileDialog1.FileName;
        //}
        //private void PopulateData(string getImageId, string BomNo)
        //{
        //    string fileName = string.Empty;
        //    byte[] content = null;

        //    try
        //    {
        //        if (getImageId.Trim().Length == 0)
        //        {
        //            dataGridView1.DataSource = null;
        //            if (Database.OpenConnection(Utility.ProductionConnectionString))
        //            {
        //                Database.myadapter = Database.GetAdapterCommand("Select FileId,FileName from BOM1 WITH(nolock) where srno = '" +
        //                    BomNo + "'");
        //                DataSet dataset = new DataSet();
        //                Database.myadapter.Fill(dataset, "StreamTable");
        //                dataGridView1.DataSource = dataset.Tables[0];
        //                currencyManager = (CurrencyManager)BindingContext[dataset.Tables[0]];
        //                Database.Closeconnection();
        //            }
        //        }
        //        else
        //        {
        //            if (Database.OpenConnection(Utility.ProductionConnectionString))
        //            {
        //                Database.myreader = Database.GetExecuteReaderCommand("Select FileName,FileStream from BOM1 WITH(nolock) where Fileid=" + getImageId
        //                          + " and srno = '" + BomNo + "'");
        //                while (Database.myreader.Read())
        //                {
        //                    fileName = (string)Database.myreader.GetString(0);
        //                    content = (byte[])Database.myreader.GetValue(1);
        //                    break;
        //                }


        //                DirectoryInfo di = new DirectoryInfo(Path.GetTempPath() + "\\Test");
        //                if (di.Exists == false)
        //                {
        //                    di.Create();
        //                }
        //                //else
        //                //{
        //                //    di.Delete(true);
        //                //    di.Create();
        //                //}


        //                FileStream fs = new FileStream(di + "\\" + fileName, FileMode.Create);
        //                fs.Write(content, 0, System.Convert.ToInt32(content.Length));
        //                fs.Seek(0, SeekOrigin.Begin);
        //                fs.Close();
        //                ProcessStartInfo psi = new ProcessStartInfo(di + "\\" + fileName);
        //                Process.Start(psi);
        //                Database.myreader.Close();
        //                Database.Closeconnection();
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        dataGridView1.Columns[1].Width = 340;
        //        dataGridView1.Columns[0].ReadOnly = true;
        //        dataGridView1.Columns[1].ReadOnly = true;

        //    }

        //}
        private void GetFile(int FileId, string fileName)
        {
            byte[] content = ReadFileToByteArray(fileName);
            FileStoreToDataBase(FileId, content, fileName);
            //PopulateData("", "");
        }
        public void FileStoreToDataBase(int FileId, byte[] content, string fileName)
        {
            try
            {
                if (Database.OpenConnection(Utility.ProductionConnectionString))
                {
                    Database.GetExecuteNonQueryCommand("insert into BOM1 (FileID,[FileName],[FileStream]) values ("
                                     + FileId + ",'" + fileName + "','" + content + "')");
                    Database.Closeconnection();
                }
            }
            finally
            {

            }
        }
        protected static byte[] ReadFileToByteArray(string fileName)
        {
            FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Read);
            long len;
            len = fileStream.Length;
            Byte[] fileAsByte = new Byte[len];
            fileStream.Read(fileAsByte, 0, fileAsByte.Length);
            MemoryStream memoryStream = new MemoryStream(fileAsByte);
            return memoryStream.ToArray();
        }
        private void dataGridView1_DoubleClick(object sender, EventArgs e)
        {
            if (currencyManager == null)
            {
                return;
            }
            if (currencyManager.Current.GetType() != typeof(DataRowView))
            {
                return;
            }
            DataRowView dataRowView = (DataRowView)currencyManager.Current;
            //if (btnUpdate.Enabled == true)
            //   // PopulateData(Convert.ToString(dataRowView["FileId"]), BOMNo);
            //else
            //  PopulateData(Convert.ToString(dataRowView["FileId"]), "");
        }
        private void comboDocType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkBoxdocpouch.Checked)
            {
                if (comboDocType.SelectedIndex == 0)
                    textDoc.Text = "0.05";
                else if (comboDocType.SelectedIndex == 1)
                    textDoc.Text = "0.12";
            }
            else
                textDoc.Text = "";
        }
        private void comboPrintType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {

                if (textQty.Text != "" && comboPrintType.SelectedIndex < 0)
                {

                    double StreoPrice = 0;
                    if (_BagQty <= 50)
                        StreoPrice = 0.440;
                    else if (_BagQty <= 400)
                        StreoPrice = 0.056;
                    else if (_BagQty <= 1000)
                        StreoPrice = 0.022;
                    else if (_BagQty <= 2000)
                        StreoPrice = 0.011;
                    else if (_BagQty <= 5000)
                        StreoPrice = 0.004;
                    else
                        StreoPrice = 0.002;

                    double PrintingPrice = 0;


                    if (comboPrintType.SelectedIndex == 0) //Unprinted
                        textPrintingRate.Text = "0";
                    else if (comboPrintType.SelectedIndex == 1)
                    {
                        if (_BagQty <= 50)
                            PrintingPrice = StreoPrice + .120;
                        else if (_BagQty <= 400)
                            PrintingPrice = StreoPrice + .079;
                        else if (_BagQty <= 1000)
                            PrintingPrice = StreoPrice + 0.045;
                        else if (_BagQty <= 2000)
                            PrintingPrice = StreoPrice + 0.034;
                        else if (_BagQty <= 5000)
                            PrintingPrice = StreoPrice + 0.027;
                        else
                            PrintingPrice = StreoPrice + 0.025;

                        textPrintingRate.Text = PrintingPrice.ToString();
                    }
                    else if (comboPrintType.SelectedIndex == 2 || comboPrintType.SelectedIndex == 6)
                    {
                        if (_BagQty <= 50)
                            PrintingPrice = StreoPrice + 0.150;
                        else if (_BagQty <= 400)
                            PrintingPrice = StreoPrice + 0.101;
                        else if (_BagQty <= 1000)
                            PrintingPrice = StreoPrice + 0.067;
                        else if (_BagQty <= 2000)
                            PrintingPrice = StreoPrice + 0.056;
                        else if (_BagQty <= 5000)
                            PrintingPrice = StreoPrice + 0.049;
                        else
                            PrintingPrice = StreoPrice + 0.047;

                        textPrintingRate.Text = PrintingPrice.ToString();
                    }
                    else if (comboPrintType.SelectedIndex == 3 || comboPrintType.SelectedIndex == 7)
                    {
                        if (_BagQty <= 50)
                            PrintingPrice = StreoPrice + 0.180;
                        else if (_BagQty <= 400)
                            PrintingPrice = StreoPrice + 0.124;
                        else if (_BagQty <= 1000)
                            PrintingPrice = StreoPrice + 0.090;
                        else if (_BagQty <= 2000)
                            PrintingPrice = StreoPrice + 0.079;
                        else if (_BagQty <= 5000)
                            PrintingPrice = StreoPrice + 0.072;
                        else
                            PrintingPrice = StreoPrice + 0.070;

                        textPrintingRate.Text = PrintingPrice.ToString();
                    }
                    else if (comboPrintType.SelectedIndex == 14 || comboPrintType.SelectedIndex == 15)
                    {
                        if (_BagQty <= 50)
                            PrintingPrice = StreoPrice + 0.210;
                        else if (_BagQty <= 400)
                            PrintingPrice = StreoPrice + 0.146;
                        else if (_BagQty <= 1000)
                            PrintingPrice = StreoPrice + 0.112;
                        else if (_BagQty <= 2000)
                            PrintingPrice = StreoPrice + 0.101;
                        else if (_BagQty <= 5000)
                            PrintingPrice = StreoPrice + 0.094;
                        else
                            PrintingPrice = StreoPrice + 0.092;

                        textPrintingRate.Text = PrintingPrice.ToString();
                    }
                    else
                    {
                        PrintingPrice = StreoPrice + 0.10;
                        textPrintingRate.Text = PrintingPrice.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void btnMail_Click(object sender, EventArgs e)
        {
            FrmSendMail.inqno = textpono.Text;
            FrmSendMail frm = new FrmSendMail();
            frm.Show();
        }
        private void checkAncerieLoop_CheckedChanged(object sender, EventArgs e)
        {
            if (checkAncerieLoop.Checked)
                groupAncillary.Visible = true;
            else
                groupAncillary.Visible = false;
        }
        private void button1_Click_1(object sender, EventArgs e)
        {
            if (comboBody2.SelectedIndex == 4) //Builder Bag
            {
                if (checkBoxliner.Checked)
                    textInstruction.Text = "1.	ALL FABRIC FOLD OUTSIDE. " + Environment.NewLine +
                      "2.	LOOP ATTACHMENT INSIDE THE PANEL." + Environment.NewLine +
                      "3.	BOX WITH SAFETY 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE.";
                else
                    textInstruction.Text = "1.	ALL FABRIC FOLD INSIDE. " + Environment.NewLine +
                     "2.	LOOP ATTACHMENT INSIDE THE PANEL." + Environment.NewLine +
                     "3.	BOX WITH SAFETY 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE.";
            }
            else if (comboBody2.SelectedIndex == 5) //Tunnel Bag
            {
                if (checkBoxliner.Checked)
                    textInstruction.Text = "1.	ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
                       "2.	BODY HEMMED WITH DOUBLE FOLD & SIDE PANEL WITH SINGLE FOLD. " + Environment.NewLine
                       + "3.	LOOP ATTACHMENT INSIDE THE PANEL." + Environment.NewLine +
                       "4.	BOX WITH SAFETY 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE.";
                else
                    textInstruction.Text = "1.	ALL FABRIC FOLD INSIDE." + Environment.NewLine +
                          "2.	BODY HEMMED WITH DOUBLE FOLD & SIDE PANEL WITH SINGLE FOLD. " + Environment.NewLine
                     + "3.	LOOP ATTACHMENT INSIDE THE PANEL." + Environment.NewLine +
                      "4.	BOX WITH SAFETY 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE.";
            }

            else if (_BodyIndex1 == 3 && checkBoxLam.Checked && checkFillerCord.Checked) //4 Panel Laminated + FillerCord
            {
                textInstruction.Text = "1. ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
                      "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE. " + Environment.NewLine
                       + "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine +
                       "4. D/S ATTACHMENT WITH DOUBLE NEEDLE." + Environment.NewLine +
                       " 5. ALL SEAM DOUBLE DUST PROOF.";
            }

            else if (_BodyIndex1 == 3 && checkBoxLam.Checked) //4 Panel Laminated
            {
                textInstruction.Text = "1. ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
                    "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine +
                     "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine +
                     "4. D/S ATTACHMENT WITH DOUBLE NEEDLE.";
            }
            else if (_BodyIndex1 == 3 && !checkBoxLam.Checked) //4 Panel UnLaminated
            {
                if (checkBoxliner.Checked)
                    textInstruction.Text = "1. ALL FABRIC FOLD OUTSIDE. " + Environment.NewLine +
                      "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine +
                      "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine +
                       "4. D/S SINGLE + DOUBLE.";
                else
                    textInstruction.Text = "1. ALL FABRIC FOLD INSIDE. " + Environment.NewLine +
                           "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine +
                           "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine +
                            "4. D/S SINGLE + DOUBLE.";
            }


            else if (_BodyIndex1 == 1 && checkBoxLam.Checked && checkFillerCord.Checked) //Circular Bags Laminated + FillerCord
            {
                textInstruction.Text = "1.	LOOP ATTACHMENT WITH 6 PASS ZUKI." + Environment.NewLine +
                        "2.	ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
                    "3.	BOTTOM WITH 2700 DNR. MW CHAIN + 4000 DNR. BLUE HIRACAL." + Environment.NewLine
                      + "4.	D/S ATTACHMENT WITH DOUBLE NEEDLE. " + Environment.NewLine
                     + "5.	ALL SEAMS WITH DOUBLE DUST PROOF.";

            }

            else if (_BodyIndex1 == 1 && checkBoxLam.Checked) // Circular Bags Laminated
            {
                textInstruction.Text = "1.	LOOP ATTACHMENT WITH 6 PASS ZUKI." + Environment.NewLine +
                     "2.	ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
                     "3.	BOTTOM WITH 2700 DNR. MW CHAIN + 4000 DNR. BLUE HIRACAL." + Environment.NewLine +
                     "4.	D/S ATTACHMENT WITH DOUBLE NEEDLE.";

            }
            else if (_BodyIndex1 == 1 && !checkBoxLam.Checked) //Circular Bags UnLaminated
            {
                if (checkBoxliner.Checked)
                    textInstruction.Text = "1.	LOOP ATTACHMENT WITH 6 PASS ZUKI." + Environment.NewLine
              + "2.	ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
              "3.	BOTTOM WITH 2700 DNR. MW CHAIN + 4000 DNR. BLUE HIRACAL." + Environment.NewLine
              + "4.	D/S SINGLE + DOUBLE.";
                else
                    textInstruction.Text = "1.	LOOP ATTACHMENT WITH 6 PASS ZUKI." + Environment.NewLine
                     + "2.	ALL FABRIC FOLD INSIDE." + Environment.NewLine +
                     "3.	BOTTOM WITH 2700 DNR. MW CHAIN + 4000 DNR. BLUE HIRACAL." + Environment.NewLine
                     + "4.	D/S SINGLE + DOUBLE.";

            }



            else if (_BodyIndex1 == 0 && checkBoxLam.Checked && checkFillerCord.Checked) //Upanel Bags Laminated + FillerCord
            {
                textInstruction.Text = "1.	ALL FABRIC FOLD OUTSIDE. " + Environment.NewLine +
                    "2.	BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine
                     + "3.	LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine +
                    "4.	D/S ATTACHMENT DOUBLE NEEDLE." + Environment.NewLine
                    + "5.	ALL SEAMS WITH DOUBLE DUST PROOF.";
            }

            else if (_BodyIndex1 == 0 && checkBoxLam.Checked) // UPanel Bags Laminated
            {
                textInstruction.Text = "1.	ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
                    "2.	BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine
                + "3.	LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine
                + "4.	D/S ATTACHMENT DOUBLE NEEDLE";

            }
            else if (_BodyIndex1 == 0 && !checkBoxLam.Checked) //UPanel Bags UnLaminated
            {
                if (checkBoxliner.Checked)
                    textInstruction.Text = "1.	ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
                   "2.	BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine +
                   "3.	LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine
                   + "4.	D/S SINGLE + DOUBLE.";
                else
                    textInstruction.Text = "1.	ALL FABRIC FOLD INSIDE." + Environment.NewLine +
                         "2.	BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine +
                         "3.	LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine
                         + "4.	D/S SINGLE + DOUBLE.";

            }

            else if (_BodyIndex1 == 2 && (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 6) && checkBoxLam.Checked && checkFillerCord.Checked) //4 Panel + Buffle Bags Laminated + FillerCord
            {
                textInstruction.Text = "1. ALL FABRIC FOLD OUTSIDE." + Environment.NewLine
                 + "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine +
                 "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine +
                 "4. D/S  DOUBLE NEEDLE." + Environment.NewLine
                 + "5. ALL SEAM DOUBLE DUST PROOF.";
            }

            else if (_BodyIndex1 == 2 && (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 6) && checkBoxLam.Checked) //4 Panel Buffle Bags Laminated
            {
                textInstruction.Text = "1. ALL FABRIC FOLD OUTSIDE." + Environment.NewLine
                     + "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine
                     + "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine
                        + " 4. D/S  DOUBLE NEEDLE.";

            }
            else if (_BodyIndex1 == 2 && (comboBuffleType.SelectedIndex == 0 || comboBuffleType.SelectedIndex == 6) && !checkBoxLam.Checked) //4 Panel Buffle Bags UnLaminated
            {
                if (checkBoxliner.Checked)
                    textInstruction.Text = "1. ALL FABRIC FOLD OUTSIDE." + Environment.NewLine
               + "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine
               + "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine
               + "4. D/S SINGLE + DOUBLE";
                else
                    textInstruction.Text = "1. ALL FABRIC FOLD INSIDE." + Environment.NewLine
                     + "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine
                     + "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine
                     + "4. D/S SINGLE + DOUBLE";

            }


            else if (_BodyIndex1 == 2 && (comboBuffleType.SelectedIndex == 4 || comboBuffleType.SelectedIndex == 5)
                  && checkBoxLam.Checked && checkFillerCord.Checked) //2 Panel + Buffle Bags Laminated + FillerCord
            {
                textInstruction.Text = "1. ALL FABRIC FOLD OUTSIDE." + Environment.NewLine
                  + "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine
                  + "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine
                  + "4. D/S  DOUBLE NEEDLE." + Environment.NewLine
                  + "5. ALL SEAM DOUBLE DUST PROOF.";
            }

            else if (_BodyIndex1 == 2 && (comboBuffleType.SelectedIndex == 4 || comboBuffleType.SelectedIndex == 5) && checkBoxLam.Checked) //2 Panel Buffle Bags Laminated
            {
                textInstruction.Text = "1. ALL FABRIC FOLD OUTSIDE." + Environment.NewLine
                       + "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine
                       + "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine
                       + "4. D/S  DOUBLE NEEDLE.";

            }
            else if (_BodyIndex1 == 2 && (comboBuffleType.SelectedIndex == 4 || comboBuffleType.SelectedIndex == 5)
                && !checkBoxLam.Checked) //2 Panel Buffle Bags UnLaminated
            {
                if (checkBoxliner.Checked)
                    textInstruction.Text = "1. ALL FABRIC FOLD OUTSIDE." + Environment.NewLine
                            + "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine
                 + "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine
                + "4. D/S SINGLE + DOUBLE.";
                else
                    textInstruction.Text = "1. ALL FABRIC FOLD INSIDE." + Environment.NewLine
                                 + "2. BOX ATTACHMENT WITH 2700 DNR. M.W.  & HIRAKEL 4000 DNR. BLUE." + Environment.NewLine
                      + "3. LOOP ATTACHMENT OUTSIDE THE PANEL." + Environment.NewLine
                     + "4. D/S SINGLE + DOUBLE.";

            }
            else if (_BodyIndex1 == 5 && !checkBoxLam.Checked)
            {
                if (checkBoxliner.Checked)
                    textInstruction.Text = " 1.	ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
               "2.	BOTTOM WITH SAFETY 2700 DNR. M.W. + 4000 DNR. BLUE HIRACAL. ";
                else
                    textInstruction.Text = " 1.	ALL FABRIC FOLD INSIDE." + Environment.NewLine +
                    "2.	BOTTOM WITH SAFETY 2700 DNR. M.W. + 4000 DNR. BLUE HIRACAL. ";
            }

            else if (_BodyIndex1 == 6 && !checkBoxLam.Checked)
            {
                if (checkBoxliner.Checked)
                    textInstruction.Text = " 1.	ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
                "2.	BOTTOM WITH SAFETY 2700 DNR. M.W. + 4000 DNR. BLUE HIRACAL. ";
                else
                    textInstruction.Text = " 1.	ALL FABRIC FOLD INSIDE." + Environment.NewLine +
                    "2.	BOTTOM WITH SAFETY 2700 DNR. M.W. + 4000 DNR. BLUE HIRACAL. ";

            }
            else if (_BodyIndex1 == 6 && checkBoxLam.Checked)
            {
                textInstruction.Text = " 1.	ALL FABRIC FOLD OUTSIDE." + Environment.NewLine +
                "2.	BOTTOM WITH SAFETY 2700 DNR. M.W. + 4000 DNR. BLUE HIRACAL. ";
            }


            if (checkBoxblock.Checked)
                textInstruction.Text += Environment.NewLine + " B-Lock";
            if (checkBoxCableTie.Checked)
                textInstruction.Text += Environment.NewLine + " Top Cable Tie";
            if (checkTopHoseSlider.Checked)
                textInstruction.Text += Environment.NewLine + " Top Hose Slider";
            if (checkBoxbottomcabletie.Checked)
                textInstruction.Text += Environment.NewLine + " Botton Cable Tie";
            if (checkBottomhoseslider.Checked)
                textInstruction.Text += Environment.NewLine + " Bottom Hose Slider";
            if (checkBoxCableTie.Checked)
                textInstruction.Text += Environment.NewLine + " Top Cable Tie";
            if (checkBoxliner.Checked)
            {
                if (combolinersubtype.SelectedIndex == 1 || combolinersubtype.SelectedIndex == 2)
                    textInstruction.Text += Environment.NewLine + combolinersubtype.Text + " "
                        + comboBoxlineratpoint.Text + " points";
            }
        }
        private void comboSpoutDia_KeyUp(object sender, KeyEventArgs e)
        {
            if (comboTopType.SelectedIndex == 1) //Top Spout
                textTopRemarks.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboSpoutDia.Text) - 5);
        }
        private void comboBoxbottomtype_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkBottomSpoutTie.Checked = false;
            if (comboBoxbottomtype.SelectedIndex == 3) //Bottom Spout
            {
                if (_BodyIndex1 == 0)
                    textBodyRemarks.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                else
                    textBottomrem.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                comboBoxbottomsubtype.SelectedIndex = 0;
            }
            //if (IsupdateMode == false) 18.09.2021
            {
                if ((comboBoxbottomtype.SelectedIndex == 3 || comboBoxbottomtype.SelectedIndex == 4 || comboBoxbottomtype.SelectedIndex == 5 || comboBoxbottomtype.SelectedIndex == 8) && IsupdateMode == false)
                {
                    checkBottomSpoutTie.Checked = true;
                }
            }
        }
        private void comboBoxbottomdia_Leave(object sender, EventArgs e)
        {
            if (comboBoxbottomsubtype.SelectedIndex == 1) //Petal Closure
            {
                if (_BodyIndex1 == 0)
                    textBodyRemarks.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                else
                    textBottomrem.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
            }
            else if (comboBoxbottomtype.SelectedIndex == 8) //Bottom Spout
            {
                if (_BodyIndex1 == 0)
                    textBodyRemarks.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                else
                    textBottomrem.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
            }
        }
        private void comboLoopL_SelectedIndexChanged(object sender, EventArgs e)
        {
            return;
            //Changes by Anjul 18-4-2011
            //try
            //{
            //    if (checkBoxLoop.Checked)
            //    {
            //        double LoopLenght = 0;
            //        double BodyH = _BagHeight;
            //        //if (textBodyH.Text.Length == 0)
            //        //    BodyH = 0;
            //        //else
            //        //    BodyH = Utility.SafeConvertToDouble (textBodyH.Text.ToString());


            //        LoopLenght = (BodyH * .7) + Utility.SafeConvertToDouble(textShortLeg.Text)
            //             + (Utility.SafeConvertToDouble(comboLoopL.Text) * 2);
            //        LoopLenght = Math.Round(LoopLenght, 0, MidpointRounding.AwayFromZero);
            //        textLoopLenght.Text = LoopLenght.ToString();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.ToString());
            //}
        }
        private void textBodyW_Leave(object sender, EventArgs e)
        {
            if (textBodyL.Text != textBodyW.Text)
            {
                MessageBox.Show("Please select SIDE");
                checkSide.Checked = true;
            }
            if (_BodyIndex1 == 2) // added by manish on 20th july
            {
                if (comboType.SelectedIndex == 0 && comboBody2.SelectedIndex == 7) // Internal
                {
                    double x = Math.Round(Utility.SafeConvertToDouble(textBodyL.Text) / 3, 1);
                    double y = x + 2;
                    textBodyRemarks.Text = "Buffle Coding " + y.ToString() + "," + x.ToString() + "," + y.ToString();
                }
                else if (comboType.SelectedIndex == 0 && comboBody2.SelectedIndex != 7) // Internal
                {
                    double x = Math.Round(Utility.SafeConvertToDouble(textBodyL.Text) / 3, 1);
                    double y = x + 5.5;
                    textBodyRemarks.Text = "Buffle Coding " + y.ToString() + "," + x.ToString() + "," + y.ToString();

                }
                else
                    textBodyRemarks.Text = "";
            }
            else
                textBodyRemarks.Text = "";
        }

        private void checkFelt_CheckedChanged(object sender, EventArgs e)
        {
            if (checkFelt.Checked)
                groupFelt.Visible = true;
            else
                groupFelt.Visible = false;
        }
        private void combotopbandgrm_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Convert.ToInt32(combotopbandgrm.Text) <= 25)
                comboTopBandSize.Text = "3";
            if (Convert.ToInt32(combotopbandgrm.Text) > 25)
                comboTopBandSize.Text = "5";

        }

        private void comboSpoutType_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (comboSpoutType.SelectedIndex == 2)
                checkSpoutRope.Checked = true;
            else
                checkSpoutRope.Checked = false;

            if (comboSpoutType.SelectedIndex == 4 && IsupdateMode == false)
                checkIRISTie.Checked = true;
            else
                checkIRISTie.Checked = false;

        }

        private void checkBoxlinerBuffle_CheckedChanged(object sender, EventArgs e)
        {
            textBuffleLinerMicron.Text = "";
            if (checkBoxlinerBuffle.Checked)
            {
                textBuffleLinerMicron.Visible = true;
                lblBuffleLiner.Visible = true;
            }
            else
            {
                textBuffleLinerMicron.Visible = false;

                lblBuffleLiner.Visible = false;
            }
        }

        private void CheckHTInclusive_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckHTInclusive.Checked)
            {
                textStartSewnBaseHt.Text = "0";
                textStartSewnBaseHt.ReadOnly = true;
            }
            else
            {
                textStartSewnBaseHt.Text = "0";
                textStartSewnBaseHt.ReadOnly = false;
            }

        }


        private void AttachFile()
        {
            if (textFilePONo.Text != "")
            {
                //Tools.DMSManager.OpenReference("BOM", string.Format("{0}/{1}/AC_PMT/{2}",
                //    AccountCommonFunction.GetCompanyCode(this.CurrentPaymentEntry.CompanyName),
                //    this.CurrentPaymentEntry.Year,
                //    this.CurrentPaymentEntry.PaymentNo
                //    ));

                Tools.DMSManager.OpenReference("BOM", string.Format("{0}",
                  textFilePONo.Text
                  ));
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            AttachFile();
        }

        private void comboBoxlinertype_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBodyGSM_Leave(object sender, EventArgs e)
        {
            try
            {
                if (comboBody1.SelectedIndex == 2 || comboBody1.SelectedIndex == 3)
                {
                    if (textBodyL.Text != textBodyW.Text)
                    {
                        comboSideGSM.Text = comboBodyGSM.Text;
                    }
                }
                if (_BodyIndex1 == 2) // added by manish on 20th july
                {
                    if (comboType.SelectedIndex == 0 && comboBody2.SelectedIndex == 7) // Internal
                    {
                        double x = Math.Round(_BagWidth / 3, 1);
                        double y = x + 2;
                        textBodyRemarks.Text = "Buffle Coding " + y.ToString() + "," + x.ToString() + "," + y.ToString();
                    }
                    else if (comboType.SelectedIndex == 0 && comboBody2.SelectedIndex != 7) // Internal
                    {
                        double x = Math.Round(_BagWidth / 3, 1);
                        double y = x + 5.5;
                        textBodyRemarks.Text = "Buffle Coding " + y.ToString() + "," + x.ToString() + "," + y.ToString();

                    }
                    else
                        textBodyRemarks.Text = "";
                }
                else
                    textBodyRemarks.Text = "";
                // end 20th July
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void comboDocUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboDocUnit.SelectedIndex == 1)
            {
                if (comboDocType2.SelectedIndex == 0) //A1
                {

                    textDocL.Text = "23.6";
                    textDocW.Text = "33.32";
                    comboDocUnit.SelectedIndex = 1;

                }
                if (comboDocType2.SelectedIndex == 1) //A2
                {
                    textDocL.Text = "16.66";
                    textDocW.Text = "23.6";
                    comboDocUnit.SelectedIndex = 1;

                }
                if (comboDocType2.SelectedIndex == 2) //A3
                {

                    textDocL.Text = "11.8";
                    textDocW.Text = "16.66";
                    comboDocUnit.SelectedIndex = 1;

                }
                if (comboDocType2.SelectedIndex == 3) //A4
                {

                    textDocL.Text = "8.33";
                    textDocW.Text = "11.8";
                    comboDocUnit.SelectedIndex = 1;

                }
                if (comboDocType2.SelectedIndex == 4) //A5
                {

                    textDocL.Text = "5.9";
                    textDocW.Text = "8.33";
                    comboDocUnit.SelectedIndex = 1;

                }
                if (comboDocType2.SelectedIndex == 5) //A6
                {
                    textDocL.Text = "4.16";
                    textDocW.Text = "5.9";
                    comboDocUnit.SelectedIndex = 1;

                }
            }
            else
            {
                textDocL.Text = Math.Round((Convert.ToDouble(textDocL.Text) * 2.54), 2).ToString();
                textDocW.Text = Math.Round((Convert.ToDouble(textDocW.Text) * 2.54), 2).ToString();

            }


        }

        private void checkdocpouch1_CheckedChanged(object sender, EventArgs e)
        {

            if (checkdocpouch1.Checked)
            {
                groupBoxdocpouch1.Visible = true;
                combodoctype3.SelectedIndex = 0;
                combodoctype4.SelectedIndex = 0;
                combodoctype5.SelectedIndex = 0;
                comboDoc1Micron.Text = "80";
            }
            else
                groupBoxdocpouch1.Visible = false;


        }

        private void combodoctype5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (combodoctype5.SelectedIndex == 0) //A1
            {

                textDoc1L.Text = "23.6";
                textDoc1W.Text = "33.32";
                comboDoc1Unit.SelectedIndex = 1;

            }
            if (combodoctype5.SelectedIndex == 1) //A2
            {
                textDoc1L.Text = "16.66";
                textDoc1W.Text = "23.6";
                comboDoc1Unit.SelectedIndex = 1;

            }
            if (combodoctype5.SelectedIndex == 2) //A3
            {

                textDoc1L.Text = "11.8";
                textDoc1W.Text = "16.66";
                comboDoc1Unit.SelectedIndex = 1;

            }
            if (combodoctype5.SelectedIndex == 3) //A4
            {

                textDoc1L.Text = "8.33";
                textDoc1W.Text = "11.8";
                comboDoc1Unit.SelectedIndex = 1;

            }
            if (combodoctype5.SelectedIndex == 4) //A5
            {

                textDoc1L.Text = "5.9";
                textDoc1W.Text = "8.33";
                comboDoc1Unit.SelectedIndex = 1;

            }
            if (combodoctype5.SelectedIndex == 5) //A6
            {
                textDoc1L.Text = "4.16";
                textDoc1W.Text = "5.9";
                comboDoc1Unit.SelectedIndex = 1;

            }
        }

        private void comboDoc1Unit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboDoc1Unit.SelectedIndex == 1)
            {
                if (combodoctype5.SelectedIndex == 0) //A1
                {

                    textDoc1L.Text = "23.6";
                    textDoc1W.Text = "33.32";
                    comboDoc1Unit.SelectedIndex = 1;

                }
                if (combodoctype5.SelectedIndex == 1) //A2
                {
                    textDoc1L.Text = "16.66";
                    textDoc1W.Text = "23.6";
                    comboDoc1Unit.SelectedIndex = 1;

                }
                if (combodoctype5.SelectedIndex == 2) //A3
                {

                    textDoc1L.Text = "11.8";
                    textDoc1W.Text = "16.66";
                    comboDoc1Unit.SelectedIndex = 1;

                }
                if (combodoctype5.SelectedIndex == 3) //A4
                {

                    textDoc1L.Text = "8.33";
                    textDoc1W.Text = "11.8";
                    comboDoc1Unit.SelectedIndex = 1;

                }
                if (combodoctype5.SelectedIndex == 4) //A5
                {

                    textDoc1L.Text = "5.9";
                    textDoc1W.Text = "8.33";
                    comboDoc1Unit.SelectedIndex = 1;

                }
                if (combodoctype5.SelectedIndex == 5) //A6
                {
                    textDoc1L.Text = "4.16";
                    textDoc1W.Text = "5.9";
                    comboDoc1Unit.SelectedIndex = 1;

                }
            }
            else
            {
                textDoc1L.Text = Math.Round((Convert.ToDouble(textDoc1L.Text) * 2.54), 2).ToString();
                textDoc1W.Text = Math.Round((Convert.ToDouble(textDoc1W.Text) * 2.54), 2).ToString();

            }
        }

        private void checkdocpouch2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkdocpouch2.Checked)
            {
                groupBoxdocpouch2.Visible = true;
                combodoctype6.SelectedIndex = 0;
                combodoctype7.SelectedIndex = 0;
                combodoctype8.SelectedIndex = 0;
                comboDoc2Micron.Text = "80";
            }
            else
                groupBoxdocpouch2.Visible = false;

        }

        private void combodoctype8_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (combodoctype8.SelectedIndex == 0) //A1
            {

                textDoc2L.Text = "23.6";
                textDoc2W.Text = "33.32";
                comboDoc2Unit.SelectedIndex = 1;

            }
            if (combodoctype8.SelectedIndex == 1) //A2
            {
                textDoc2L.Text = "16.66";
                textDoc2W.Text = "23.6";
                comboDoc2Unit.SelectedIndex = 1;

            }
            if (combodoctype8.SelectedIndex == 2) //A3
            {

                textDoc2L.Text = "11.8";
                textDoc2W.Text = "16.66";
                comboDoc2Unit.SelectedIndex = 1;

            }
            if (combodoctype8.SelectedIndex == 3) //A4
            {

                textDoc2L.Text = "8.33";
                textDoc2W.Text = "11.8";
                comboDoc2Unit.SelectedIndex = 1;

            }
            if (combodoctype8.SelectedIndex == 4) //A5
            {

                textDoc2L.Text = "5.9";
                textDoc2W.Text = "8.33";
                comboDoc2Unit.SelectedIndex = 1;

            }
            if (combodoctype8.SelectedIndex == 5) //A6
            {
                textDoc2L.Text = "4.16";
                textDoc2W.Text = "5.9";
                comboDoc2Unit.SelectedIndex = 1;

            }
        }

        private void comboDoc2Unit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboDoc2Unit.SelectedIndex == 1)
            {
                if (combodoctype8.SelectedIndex == 0) //A1
                {

                    textDoc2L.Text = "23.6";
                    textDoc2W.Text = "33.32";
                    comboDoc2Unit.SelectedIndex = 1;

                }
                if (combodoctype8.SelectedIndex == 1) //A2
                {
                    textDoc2L.Text = "16.66";
                    textDoc2W.Text = "23.6";
                    comboDoc2Unit.SelectedIndex = 1;

                }
                if (combodoctype8.SelectedIndex == 2) //A3
                {

                    textDoc2L.Text = "11.8";
                    textDoc2W.Text = "16.66";
                    comboDoc2Unit.SelectedIndex = 1;

                }
                if (combodoctype8.SelectedIndex == 3) //A4
                {

                    textDoc2L.Text = "8.33";
                    textDoc2W.Text = "11.8";
                    comboDoc2Unit.SelectedIndex = 1;

                }
                if (combodoctype8.SelectedIndex == 4) //A5
                {

                    textDoc2L.Text = "5.9";
                    textDoc2W.Text = "8.33";
                    comboDoc2Unit.SelectedIndex = 1;

                }
                if (combodoctype8.SelectedIndex == 5) //A6
                {
                    textDoc2L.Text = "4.16";
                    textDoc2W.Text = "5.9";
                    comboDoc2Unit.SelectedIndex = 1;

                }
            }
            else
            {
                textDoc2L.Text = Math.Round((Convert.ToDouble(textDoc2L.Text) * 2.54), 2).ToString();
                textDoc2W.Text = Math.Round((Convert.ToDouble(textDoc2W.Text) * 2.54), 2).ToString();

            }
        }

        private void textBottomRemarks_TextChanged(object sender, EventArgs e)
        {

        }

        private void label47_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxbottomgsm_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void checkBottom1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottom1.Checked)
                groupBoxbottom1.Visible = true;
            else
                groupBoxbottom1.Visible = false;

            if (_BodyIndex1 == 1) //Circular
                comboBoxbottomgsm2.Text = Convert.ToString(Convert.ToInt32(comboBodyGSM.Text) + 10);

            else if (_BodyIndex1 == 0 || _BodyIndex1 == 2 || _BodyIndex1 == 3)
                comboBoxbottomgsm2.Text = comboBodyGSM.Text;
            else
                comboBoxbottomgsm2.Text = "70";
        }

        private void checkBottom2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottom2.Checked)
                groupBoxbottom2.Visible = true;
            else
                groupBoxbottom2.Visible = false;

            if (_BodyIndex1 == 1) //Circular
                comboBoxbottomgsm4.Text = Convert.ToString(Convert.ToInt32(comboBodyGSM.Text) + 10);

            else if (_BodyIndex1 == 0 || _BodyIndex1 == 2 || _BodyIndex1 == 3)
                comboBoxbottomgsm4.Text = comboBodyGSM.Text;
            else
                comboBoxbottomgsm4.Text = "70";
        }

        private void comboBoxbottomtype1_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkBottomSpoutTie1.Checked = false;
            if (comboBoxbottomtype1.SelectedIndex == 3) //Bottom Spout
            {
                if (_BodyIndex1 == 0)
                    textBodyRemarks.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                else
                    textBottomrem.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                comboBoxbottomsubtype1.SelectedIndex = 0;
            }
            if (comboBoxbottomtype1.SelectedIndex == 3 || comboBoxbottomtype1.SelectedIndex == 4 || comboBoxbottomtype1.SelectedIndex == 5 || comboBoxbottomtype1.SelectedIndex == 8)
            { checkBottomSpoutTie1.Checked = true; }
        }

        private void comboBoxbottomtype2_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkBottomSpoutTie2.Checked = false;
            if (comboBoxbottomtype2.SelectedIndex == 3) //Bottom Spout
            {
                if (_BodyIndex1 == 0)
                    textBodyRemarks.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                else
                    textBottomrem.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                comboBoxbottomsubtype2.SelectedIndex = 0;
            }
            if (comboBoxbottomtype2.SelectedIndex == 3 || comboBoxbottomtype2.SelectedIndex == 4 || comboBoxbottomtype2.SelectedIndex == 5 || comboBoxbottomtype2.SelectedIndex == 8)
            { checkBottomSpoutTie2.Checked = true; }
        }

        private void comboBoxbottomsubtype1_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkBottomSpoutRope1.Checked = false;
            comboBottomSpoutRopeGrm1.Text = "";
            comboBottomSpoutRopeSize1.Text = "";
            comboBottomSpoutRope1.SelectedIndex = 0;
            if (comboBoxbottomsubtype1.SelectedIndex != 5)
            {
                if (comboBoxbottomsubtype1.SelectedIndex == 1) //Petal Closure
                {
                    if (_BodyIndex1 == 0)
                        textBodyRemarks.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);

                    else
                        textBottomrem.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                    checkBottomSpoutRope1.Checked = true;
                    comboBottomSpoutRopeGrm1.Text = "8";
                    comboBottomSpoutRopeSize1.Text = "6";
                    comboBottomSpoutRope1.SelectedIndex = 0;

                }
                //else
                //{
                comboBoxbottomgsm3.Text = "70";
                comboBoxBottomSubTypeLamiGSM1.Text = "25";
                checkBoxbottomlam3.Checked = true;
                comboBoxbottomdia1.Text = "35";
                comboBoxbottomheight1.Text = "50";
                // }

            }
            else
            {
                comboBoxbottomgsm3.Text = "0";
                comboBoxBottomSubTypeLamiGSM1.Text = "0";
                checkBoxbottomlam3.Checked = false;
                comboBoxbottomdia1.Text = "0";
                comboBoxbottomheight1.Text = "0";
            }
        }

        private void comboBoxbottomsubtype2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxbottomsubtype2.SelectedIndex != 5)
            {
                if (comboBoxbottomsubtype2.SelectedIndex == 1) //Petal Closure
                {
                    if (_BodyIndex1 == 0)
                        textBodyRemarks.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);

                    else
                        textBottomrem.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                    checkBottomSpoutRope2.Checked = true;
                    comboBottomSpoutRopeGrm2.Text = "8";
                    comboBottomSpoutRopeSize2.Text = "6";
                    comboBottomSpoutRope2.SelectedIndex = 0;

                }
                //else
                //{
                comboBoxbottomgsm5.Text = "70";
                comboBoxBottomSubTypeLamiGSM2.Text = "25";
                checkBoxbottomlam5.Checked = true;
                comboBoxbottomdia2.Text = "35";
                comboBoxbottomheight2.Text = "50";
                // }

            }
            else
            {
                comboBoxbottomgsm5.Text = "0";
                comboBoxBottomSubTypeLamiGSM2.Text = "0";
                checkBoxbottomlam5.Checked = false;
                comboBoxbottomdia2.Text = "0";
                comboBoxbottomheight2.Text = "0";
            }
        }

        private void comboBoxbottomdia1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_BodyIndex1 == 0 && comboBoxbottomtype1.SelectedIndex == 3)
                textBodyRemarks.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia1.Text) - 5);
            else if (_BodyIndex1 == 0 && comboBoxbottomtype1.SelectedIndex == 1)
                textBodyRemarks.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia1.Text) - 5);

            else
            {
                if (comboBoxbottomsubtype1.SelectedIndex == 1) //Petal Closure
                {
                    if (_BodyIndex1 == 3 || _BodyIndex1 == 1)
                        textBottomrem1.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia1.Text) - 5);
                    // else
                    //   textBottomRemarks.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                }

                else if (comboBoxbottomtype1.SelectedIndex == 3) //Bottom Spout
                {
                    textBottomrem1.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia1.Text) - 5);
                }
            }
        }

        private void comboBoxbottomdia2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_BodyIndex1 == 0 && comboBoxbottomtype2.SelectedIndex == 3)
                textBodyRemarks.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia2.Text) - 5);
            else if (_BodyIndex1 == 0 && comboBoxbottomtype2.SelectedIndex == 1)
                textBodyRemarks.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia2.Text) - 5);

            else
            {
                if (comboBoxbottomsubtype2.SelectedIndex == 1) //Petal Closure
                {
                    if (_BodyIndex1 == 3 || _BodyIndex1 == 1)
                        textBottomrem2.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia2.Text) - 5);
                    // else
                    //   textBottomRemarks.Text = " CROSS PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia.Text) - 5);
                }

                else if (comboBoxbottomtype2.SelectedIndex == 3) //Bottom Spout
                {
                    textBottomrem2.Text = " ROUND PUNCH - " + Convert.ToString(Convert.ToInt32(comboBoxbottomdia2.Text) - 5);
                }
            }
        }

        private void checkBoxbottomtieextra_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBottomSpoutRope1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomSpoutRope1.Checked)
            {
                groupBottomSpoutRope1.Visible = true;
                comboBottomSpoutRopeGrm1.Text = "8";
                comboBottomSpoutRopeSize1.Text = "6";

            }
            else
                groupBottomSpoutRope1.Visible = false;
        }

        private void checkBottomSpoutRope2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomSpoutRope2.Checked)
            {
                groupBottomSpoutRope2.Visible = true;
                comboBottomSpoutRopeGrm2.Text = "8";
                comboBottomSpoutRopeSize2.Text = "6";

            }
            else
                groupBottomSpoutRope2.Visible = false;
        }

        private void tabPage8_Click(object sender, EventArgs e)
        {

        }

        private void checkBottomloop_CheckedChanged(object sender, EventArgs e)
        {

            if (checkBottomloop.Checked)
                groupBottomLoop.Visible = true;
            else
                groupBottomLoop.Visible = false;
        }

        private void checkIRISRope_CheckedChanged(object sender, EventArgs e)
        {
            if (checkIRISRope.Checked)
            {
                groupTopSpoutRope.Visible = true;
                comboSpoutRope.SelectedIndex = 0;
                comboTopSpoutRopeGrm.Text = "8";
                comboSpoutRopeSize.Text = "6";
            }
            else
                groupTopSpoutRope.Visible = false;
        }

        private void checkIRISTie_CheckedChanged(object sender, EventArgs e)
        {
            if (checkIRISTie.Checked)
            {
                groupTopSpoutIRISTie.Visible = true;
                textTopSpoutTieIRISRemarks.Text = "Size :" + comboSpoutTieIRISCutSize.Text;

            }
            else
                groupTopSpoutIRISTie.Visible = false;
        }

        private void checkBottomspoutirisrope_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomspoutirisrope.Checked)
            {
                groupBottomSpoutRope.Visible = true;
                comboBottomSpoutRopeGrm.Text = "8";
                comboBottomSpoutRopeSize.Text = "6";

            }
            else
                groupBottomSpoutRope.Visible = false;
        }

        private void checkBottomspoutiristie_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomspoutiristie.Checked)
            {
                groupBottomSpoutIRISTie.Visible = true;
                textBottomSpoutTieIRISRemarks.Text = "Size :" + comboBottomSpoutTieIRISCutSize.Text;
            }
            else
                groupBottomSpoutIRISTie.Visible = false;
        }

        private void textQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
                {
                    e.Handled = true;
                }
                // only allow one decimal point
                if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                e.Handled = false;
            }
        }

        private void comboBuType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBuType.SelectedIndex == 1 || comboBuType.SelectedIndex == 2 || comboBuType.SelectedIndex == 3)
                grpBufType.Visible = true;
            else
            {
                grpBufType.Visible = false;
                txtBuffSideA.Text = "0";
                txtBuffSideB.Text = "0";
            }

            if (comboBuType.SelectedIndex == 1)
            {
                cmbSubBufType.Visible = true;
                lblSubBufType.Visible = true;
                if (cmbSubBufType.SelectedIndex == 1)
                {
                    grpBufType.Visible = true;
                }
                else
                {
                    grpBufType.Visible = false;
                    txtBuffSideA.Text = "0";
                    txtBuffSideB.Text = "0";
                }
            }
            else
            {
                cmbSubBufType.Text = "";
                cmbSubBufType.Visible = false;
                lblSubBufType.Visible = false;
                txtBuffSideA.Text = "0";
                txtBuffSideB.Text = "0";
            }
        }
        /// <summary>
        /// checkBoxTopBellyBand2_CheckedChanged
        /// 18.06.2021
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxTopBellyBand2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTopBellyBand2.Checked)
                groupBoxtopBellyband2.Visible = true;
            else
                groupBoxtopBellyband2.Visible = false;
        }
        /// <summary>
        /// checkBoxTopBellyBand1_CheckedChanged
        /// 18.06.2021
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxTopBellyBand1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTopBellyBand1.Checked)
                groupBoxtopBellyband1.Visible = true;
            else
                groupBoxtopBellyband1.Visible = false;
        }
        /// <summary>
        /// checkBoxTopBottomBand_CheckedChanged
        /// 18.06.2021
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxTopBottomBand_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTopBottomBand.Checked)
                groupBoxtopBottomBand.Visible = true;
            else
                groupBoxtopBottomBand.Visible = false;
        }

        private void cmbSubBufType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSubBufType.SelectedIndex == 1)
                grpBufType.Visible = true;
            else
            {
                grpBufType.Visible = false;
                txtBuffSideA.Text = "0";
                txtBuffSideB.Text = "0";
            }
        }

        private void chkfabricp_CheckedChanged(object sender, EventArgs e)
        {
            cmbfabricPatchLamGSM.Visible = chkfabricp.Checked;
        }

        private void chkFabricPatch_CheckedChanged(object sender, EventArgs e)
        {
            cmbfabricpatchGSM.Visible = chkFabricPatch.Checked;
            cmbfabricPatchLamGSM.Visible = chkFabricPatch.Checked;
        }

        private void checkFeltMfwebbing_CheckedChanged(object sender, EventArgs e)
        {
            if (checkFeltMfwebbing.Checked)
                groupMFWeb.Visible = true;
            else
                groupMFWeb.Visible = false;
        }

        private int GetTieNos(string gsm, string Cutsize, double totalkg)
        {
            try
            {
                double nos = Utility.SafeConvertToDouble(totalkg / ((Utility.SafeConvertToDouble(gsm) / 1000) * (Utility.SafeConvertToDouble(Cutsize)))) * 100;
                return Convert.ToInt32(nos);
            }
            catch
            {
                return 1;
            }
        }

        private void callOldBom()
        {
            //#region bom
            //bool isBodylami = false;

            //DataTable DtFillBOM = new DataTable();
            //SqlDataAdapter myadpter1 = Database.GetAdapterCommand("Select   dbo.BOM.Heading  ,dbo.BOM.GSM , dbo.BOM.Lami , dbo.BOM.Color , " +
            //" dbo.BOM.FabricSize , dbo.BOM.CutSize , dbo.BOM.TotalMtr , dbo.BOM.TotalKg as HeadTotalKG ,BOM.Remarks,dbo.BOM.PONo,dbo.BOM.SrNo  " +
            //" from BOM WITH(nolock) where dbo.BOM.PONo='" + textFilePONo.Text + "'");

            //myadpter1.Fill(DtFillBOM);

            //int index = 0;
            //dataGridView1.Rows.Clear();
            //for (int i = 0; i < DtFillBOM.Rows.Count; i++)
            //{

            //    if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top")  // Top read
            //    {


            //    }                
            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom")  // Bottom read
            //    {


            //    }
            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Spout")  // Top Spout/ FS Spout read
            //    {

            //    }
            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Top Spout Tie")
            //    {
            //        textTopTieNo.Text = GetTieNos(DtFillBOM.Rows[i]["GSM"].ToString().Trim(), DtFillBOM.Rows[i]["Cutsize"].ToString().Trim(),
            //                   Utility.SafeConvertToDouble(DtFillBOM.Rows[i]["HeadTotalKG"].ToString().Trim())).ToString();
            //    }
            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "IRIS Tie")  //  FSTie 29.09.2021
            //    {


            //    }

            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout")  // Bottom Spout
            //    {


            //    }
            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout1")  // Bottom Spout
            //    { 

            //    }
            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout2")  // Bottom Spout
            //    {

            //    }

            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout Tie")  // Bottom Spout 29.09.2021
            //    {

            //    }
            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "IRIS Bottom Tie")  // Bottom Spout 29.09.2021
            //    {

            //    }
            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout Tie1")  // Bottom Spout
            //    {

            //    }
            //    else if (DtFillBOM.Rows[i]["Heading"].ToString() == "Bottom Spout Tie2")  // Bottom Spout
            //    {

            //    }






            //}
            //#endregion
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_MouseLeave(object sender, EventArgs e)
        {

        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (checkDocFlap.Checked)
                textDocRemarks.Text = "FLAP " + textDocFlapsize.Text + " CMS";
            else
                textDocRemarks.Text = "";
        }

        private void comboDocType_Leave(object sender, EventArgs e)
        {
            if (comboDocType.SelectedIndex == 1 || comboDocType.SelectedIndex == 2)
                textDocRemarks.Text = comboDocType.Text;
            else
                textDocRemarks.Text = "";
        }

        private void checkStevedorecover_CheckedChanged(object sender, EventArgs e)
        {
            if (checkStevedorecover.Checked)
                groupStevedorecover.Visible = true;
            else
                groupStevedorecover.Visible = false;
        }

        private void checkBoxDoubleFoldTop_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDoubleFoldTop.Checked)
                textTopSpoutRemarks.Text = "Double Fold";
            else
                textTopSpoutRemarks.Text = "";
        }

        private void checkTopEdgeHemming_CheckedChanged(object sender, EventArgs e)
        {

            if (checkTopEdgeHemming.Checked)
                textTopSpoutRemarks.Text = "Edge Hemming";
            else
                textTopSpoutRemarks.Text = "";
        }

        private void checkBoxDoubleFoldBottom_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDoubleFoldBottom.Checked)
                textBottomRemarks.Text = "Double Fold";
            else
                textBottomRemarks.Text = "";
        }

        private void checkBottomEdgeHemming_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBottomEdgeHemming.Checked)
                textBottomRemarks.Text = "Edge Hemming";
            else
                textBottomRemarks.Text = "";
        }

        private void comboSpoutTieCutSize_Leave(object sender, EventArgs e)
        {
            textTopSpoutTieRemarks.Text = "Size: " + comboSpoutTieCutSize.Text;
        }

        private void comboBottomSpoutTieCutSize_Leave(object sender, EventArgs e)
        {
            textBottomSpoutTieRemarks.Text = "Size: " + comboBottomSpoutTieCutSize.Text;
        }

        private void comboBottomSpoutTieIRISCutSize_Leave(object sender, EventArgs e)
        {
            textBottomSpoutTieIRISRemarks.Text = "Size :" + comboBottomSpoutTieIRISCutSize.Text;
        }

        private void comboSpoutTieIRISCutSize_Leave(object sender, EventArgs e)
        {
            textTopSpoutTieIRISRemarks.Text = "Size :" + comboSpoutTieIRISCutSize.Text;

        }

        private void label387_Click(object sender, EventArgs e)
        {

        }

        private void TextFillerGPM_TextChanged(object sender, EventArgs e)
        {

        }

        private void label253_Click(object sender, EventArgs e)
        {

        }

        private void textShortLeg_TextChanged(object sender, EventArgs e)
        {

        }

        private void LongLegFormula()
        {
            try
            {
                if (comboLoopConst.SelectedIndex == 1) // Corner
                {
                    if (textLongLeg.Text.Length > 0 && textShortLeg.Text.Length > 0 && comboLoopL.Text.Length > 0)
                    {
                        //if (checkBoxDropLoop.Checked)
                        //    textLoopLenght.Text = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2 + Utility.SafeConvertToDouble(textShortLeg.Text)
                        //    + Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2
                        //    + Utility.SafeConvertToDouble(textLongLeg.Text)).ToString();
                        //else
                            textLoopLenght.Text = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2 + Utility.SafeConvertToDouble(textShortLeg.Text)
                                + Utility.SafeConvertToDouble(textLongLeg.Text)).ToString();
                    }
                    else
                        textLoopLenght.Text = "";
                }
                else if (comboLoopConst.SelectedIndex == 2) // Cross Corner
                {

                    if (textLongLeg.Text.Length > 0 && textShortLeg.Text.Length > 0 && comboLoopL.Text.Length > 0)
                    {
                        //if (checkBoxDropLoop.Checked)
                        //    textLoopLenght.Text = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2
                        //    + Utility.SafeConvertToDouble(textLoopDropLenght.Text) * 2
                        //    + Utility.SafeConvertToDouble(textLongLeg.Text) * 2).ToString();
                        //else
                            textLoopLenght.Text = (Utility.SafeConvertToDouble(comboLoopL.Text) * 2
                                + Utility.SafeConvertToDouble(textLongLeg.Text) * 2).ToString();
                        textLoopLenght.ReadOnly = true;
                    }
                    else
                        textLoopLenght.Text = "";
                }
                else if (comboLoopConst.SelectedIndex == 3) // full loop Corner
                {
                    textLoopLenght.ReadOnly = false;
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void textLongLeg_Leave(object sender, EventArgs e)
        {
            LongLegFormula();
        }

        private void textShortLeg_Leave(object sender, EventArgs e)
        {
            LongLegFormula();
        }

        private void textLoopDropLenght_Leave(object sender, EventArgs e)
        {
            LongLegFormula();
        }

        private void comboLoopConst_Leave(object sender, EventArgs e)
        {
            if (comboLoopConst.SelectedIndex == 3) // full loop
                textLoopLenght.ReadOnly = false;
            else
                textLoopLenght.ReadOnly = true;
        }

        private void dataGridView1_Leave(object sender, EventArgs e)
        {

        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
           



        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            try
            {
                double totalmtr = 0;
                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9].Value.ToString() == "Fabric")
                {
                    double totalkgs = 0;
                   
                    string[] s = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].Value.ToString().Split('+');
                    int lam = 0;
                    for (int i = 0; i < s.Length; i++)
                        lam += Convert.ToInt32(s[i]);


                   
                    totalkgs = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].Value)
                         * Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value) * lam;

                    totalkgs = totalkgs / 10000000;
                    totalkgs = Math.Round(totalkgs, 4);
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].Value = totalkgs.ToString();

                    totalmtr = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value) * Convert.ToDouble(textQty.Text);
                    totalmtr = totalmtr / 100;
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].Value = totalmtr.ToString();
                }

                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9].Value.ToString() == "Webbing/Tie")
                {
                    double totalkgs = 0;

                    string[] s = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].Value.ToString().Split('+');
                    int lam = 0;
                    for (int i = 0; i < s.Length; i++)
                        lam += Convert.ToInt32(s[i]);


                    totalkgs = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value) * lam;

                    totalkgs = totalkgs / 100000;
                    totalkgs = Math.Round(totalkgs, 4);
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].Value = totalkgs.ToString();

                    totalmtr = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value) * Convert.ToDouble(textQty.Text);
                    totalmtr = totalmtr / 100;
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].Value = totalmtr.ToString();

                }

                if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[9].Value.ToString() == "Liner")
                {
                    double totalkgs = 0;

                    string[] s = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[1].Value.ToString().Split('+');
                    int lam = 0;
                    for (int i = 0; i < s.Length; i++)
                        lam += Convert.ToInt32(s[i]);


                    totalkgs = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[4].Value)
                         * Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value) * lam *.92 *2;

                    totalkgs = totalkgs / 10000000;
                    totalkgs = Math.Round(totalkgs, 4);
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[7].Value = totalkgs.ToString();

                    totalmtr = Convert.ToDouble(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[5].Value) * Convert.ToDouble(textQty.Text);
                    totalmtr = totalmtr / 100;
                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[6].Value = totalmtr.ToString();
                }
            }
            catch (Exception ex)
            {
               // MessageBox.Show(ex.ToString());
            }
        }

    }
}