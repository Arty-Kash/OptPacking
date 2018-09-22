using System;
using Xamarin.Forms;

namespace OptPacking
{
    public partial class MainPage : ContentPage
    {
        // Container Position and Size
        static double X0 = 10;
        static double Y0 = 500;
        static double ContainerWidth = 200;
        static double ContainerHeight = 250;
        //static double ContainerHeight = 60;

        public MainPage()
        {
            InitializeComponent();
            InitSystem();
        }


        // Get "Console" size properly and set some variables
        void GetBoardSize(object sender, EventArgs args)
        {
            //Label1.Text = string.Format("Board Size = ( {0}, {1} )",
            //                            Board.Width, Board.Height);
        }
        

        // Initialize All of Blocks, Cranes, and Conveyor
        void InitSystem()
        {
            InitBlocks();
            InitCranes();

            ContainerBox.TranslationY = Y0 - ContainerHeight;
            ContainerBox.HeightRequest = ContainerHeight;
            ConveyorBoard.TranslationY = MaxSize;
            OperateSystemButton.Text = "Crane";

            // Calculate Block Packing Position (default: Bottom-Left)            
            BestHeight = BottomLeft(ref Blocks, "Area");
        }


        // Operate All of the System
        async void OperateSystem(object sender, EventArgs e)
        {
            Button button = (Button)sender;

            switch (button.Text)
            {
                case "Crane":
                    foreach (Crane crane in Cranes)
                        OperateCrane(crane);
                    RotateGear();
                    StatusLabel.Text = "Crane Operated";
                    button.Text = "Block";
                    break;

                case "Block":
                    Area = 0;
                    BestHeight = 0;
                    foreach (Block block in Blocks)
                        OperateBlock(block);
                    StatusLabel.Text = "Serving Blocks";
                    break;

                case "Change":
                    break;
            }
        }


        // Change Packing Algorithm
        void ChangePacking(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            double height = 0;

            switch (button.Text)
            {
                case "NFDH":    // Near-Fit Decreasing Height
                    height = XFDH("FFDH", ref Blocks);                    
                    button.Text = "FFDH";
                    break;

                case "FFDH":    // Fast-Fit Decreasing Height
                    height = XFDH("BFDH", ref Blocks);                    
                    button.Text = "BFDH";
                    break;

                case "BFDH":    // Best-Fit Decreasing Height
                    PackedBlocks.Clear();
                    height = BottomLeft(ref Blocks, "Area");
                    button.Text = "BL";
                    break;

                case "BL":      // Bottom-Left
                    height = XFDH("NFDH", ref Blocks);                    
                    button.Text = "NFDH";
                    break;
            }

            // The Mode after all blocks has been served
            if (OperateSystemButton.Text == "Change")
            {
                foreach (Block block in Blocks)
                    block.Body.TranslateTo(block.Xg, block.Yg - block.H, 1000);

                // Fit the Container size to the Packed Blocks            
                PackRatio = ExpandContainer(height);
                Label6.Text = string.Format(" max height: {0} pixel", BestHeight);
                Label7.Text = string.Format(" pack ratio: {0:0.0} %", PackRatio);
            }
        }


        // Set all the Blocks to the Container quickly (without Crane Operation)
        void SetBlockToContainer(object sender, EventArgs e)
        {
            Area = 0;
            foreach (Block block in Blocks)
            {
                ConveyorAbsLayout.Children.Remove(block.Body);
                Board.Children.Add(block.Body);
                block.Body.TranslationX = block.Xg;
                block.Body.TranslationY = block.Yg - block.H;
                block.Body.BackgroundColor = Color.White;
                Area += block.Area;
            }

            // Fit the Container size to the Packed Blocks            
            PackRatio = ExpandContainer(BestHeight);
            Label6.Text = string.Format(" max height: {0} pixel", BestHeight);
            Label7.Text = string.Format(" pack ratio: {0:0.0} %", PackRatio);

            OperateSystemButton.Text = "Change";
            StatusLabel.Text = "All Blocks Served1";
        }

    }   // End of public partial class MainPage
}       // End of namespace OptPacking
