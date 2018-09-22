//#define BoxDisplay

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;

namespace OptPacking
{
    public partial class MainPage : ContentPage
    {        
        List<Block> Blocks = new List<Block>();
        int NumBlocks = 0;  // The Number of Blocks
        int MaxSize = 50;   // Maximum Block Size
        int MinSize = 10;   // Minimum Block Size
        double Vb = 0.1;    // Block Velocity

        Random rnd = new Random();

        bool ServeBlock = false;

        // Block Class
        class Block
        {
            private ContentView body = new ContentView()
            {                
                BackgroundColor = Color.Transparent,
                Padding = 1,
                Content = new Label
                {
                    WidthRequest = 100,
                    HeightRequest = 100,
                    BackgroundColor = Color.Aqua,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            public ContentView Body { get { return this.body; } }
            public int No { get; set; }
            public string Name
            {
                get { return ((Label)Body.Content).Text; }
                set { ((Label)Body.Content).Text = value; }
            }
            public double X
            {
                get { return Body.TranslationX; }
                set { Body.TranslationX = value; }
            }
            public double Y
            {
                get { return Body.TranslationY; }
                set { Body.TranslationY = value; }
            }
            public double W
            {
                get { return Body.WidthRequest; }
                set { Body.WidthRequest = value; }
            }
            public double H
            {
                get { return Body.HeightRequest; }
                set { Body.HeightRequest = value; }
            }
            public double Area
            {
                get { return W * H; }
            }

            private double r;
            private double angle()
            {
                r = r % 360;
                if (r > 180) r -= 360;
                if (r < -180) r += 360;
                return r;
            }
            public double R
            {
                get { return this.angle(); }
                set { this.r = value; }
            }

            // Anchor value in Rotating Anchor Coordinate
            public double AnchorX { get; set; }            
            public double AnchorY { get; set; }

            // Bottom-Left position of blocks
            public double Xg { get; set; }
            public double Yg { get; set; }
            
            public bool Start { get; set; }
        }


        // Create and Initialize new Blocks
        void InitBlocks()
        {
            List<ContentView> contentViews = new List<ContentView>();
            List<BoxView> boxViews = new List<BoxView>();

            // Create Blocks with random size
            for (int i = 0; i < 100; i++)
            {
                double w = rnd.Next(MinSize, MaxSize);
                double h = rnd.Next(MinSize, MaxSize);
                Block block = new Block()
                {
                    No = i + 1,
                    Name = (i + 1).ToString(),
                    W = w,
                    H = h,
                    X = i * 150 + 200,
                    Y = MaxSize - h,                    
                };
                if(block.W < 20 || block.H < 15) // Small Font for Small Blocks
                {
                    Label label = (Label)block.Body.Content;
                    label.FontSize = 10;                    
                }
                
                // If total blocks area becomes larger than the initial Container size,
                // stop blocks creation
                if (Area + block.Area > ContainerWidth * ContainerHeight)
                {
                    NumBlocks = i;
                    break;
                }

                Area += block.Area;

                ConveyorAbsLayout.Children.Add(block.Body);
                Blocks.Add(block);


            }
            
            Label4.Text = string.Format("  # {0} [blocks]", NumBlocks);
            Label5.Text = string.Format("  tot. area: {0:#,0}", Area);
            Label6.Text = " max height: 0 pixel";
            Label7.Text = " pack ratio: 0 %";
        }


        // Serve each Block from the Conveyor to the Container
        async Task OperateBlock(Block block)
        {
            // Slide Blocks to the left on the Conveyor, 
            // and Wait Ready at the position 100 of the Conveyor
            await block.Body.TranslateTo(100, MaxSize - block.H, (uint)((block.X - 100) / Vc));            

            // A Crane approaches the Conveyor (ServeBlock = false -> true), 
            // Start the Blocks Serving
            ServeBlock = false;
            Device.StartTimer(
                TimeSpan.FromMilliseconds(1), () => {
                    if (ServeBlock)
                    {
                        block.Start = true;
                        MoveBlock(block);                        
                    }
                    
                    return !block.Start;    // true : Next Timer Start
                                            // false: Timer Stop
                }
            );
        }


        async Task MoveBlock(Block block)
        {
            // Crane Pick Up Position
            //   - Crane Position on Board:     300
            //   - Conveyor TranslationX on Board: 250
            double x = 50.0 - block.W / 2.0 + CraneWidth / 2.0;

            ServeBlock = false; // Initialize ServeBlock for the next Block

            // Move the Block to the Crane Pick Up Position, 
            // and simultaneously move the Conveyor up and down so that 
            // the Block can be lifted up by the Crane
            await Task.WhenAll(
                block.Body.TranslateTo(x, MaxSize - block.H, (uint)(100.0 / Vc)),
                Conveyor.TranslateTo(250, 150 - (MaxSize - block.H), (uint)(100.0 / Vc))
            );

            // Move the blcok from the Conveyor to the Board
            ConveyorAbsLayout.Children.Remove(block.Body);
            block.X = 250 + x;
            block.Y = 150;
            Board.Children.Add(block.Body);

            // The Block parallelly move to the left with the Crane
            await block.Body.TranslateTo(50 + x, 150, (uint)(200.0 / Vc));

            // The Block is released and falls to the Container
            await FallTo(block, block.Xg, block.Yg - block.H);

            
            if (block.No == NumBlocks)  // All Blocks has been served
            {
                OperateSystemButton.Text = "Change";
                StatusLabel.Text = "All Blocks Served1";
                CraneStart = false; // Stop the Cranes
            }
        }


        // The Block falls with rotating to the Container
        async Task FallTo(Block block, double x, double y, uint length = 0)
        {
            // Create a new Trajectory and the Block falls along it
            double rndx, rndy;
            int n = rnd.Next(2, 4);
            for (int i = 0; i < n; i++)
            {
                rndx = block.X + rnd.Next(50, 100);
                rndy = block.Y + rnd.Next(50, 100);
                await RotateToXY(block, rndx, rndy, length: length);
            }
            
            // In the final rotating, the Block Label keeps Upright
            await RotateToXY(block, x, y, true, length: length);

            // Reset Block Rotation
            block.AnchorX = 0; block.AnchorY = 0;
            block.Body.Rotation = 0;
            block.Body.Content.Rotation = 0;
            block.Body.BackgroundColor = Color.White;

            // Update maximum height of the packed blocks
            if (Y0 - y > BestHeight) BestHeight = Y0 - y;

            // If the packed blocks are full, expand the Container            
            if (BestHeight > ContainerHeight) ExpandContainer(BestHeight);

            Area += block.Area;
            PackRatio = Area / (ContainerWidth * ContainerHeight) * 100;            
            Label6.Text = string.Format(" max height: {0} pixel", BestHeight);
            Label7.Text = string.Format(" pack ratio: {0:0.0} %", PackRatio);
        }

        
        // Move Block to the position (x, y) with rotating
        async Task RotateToXY(Block block, double x, double y, 
                                bool upright = false, uint length = 0)
        {
            double x0 = block.X;        double y0 = block.Y;
            double dx = x - x0;         double dy = y - y0;
            double xc = x0 + dx / 2;    double yc = y0 + dy / 2;

            if (Math.Abs(dy) < 0.000001) dy = 0.001;
            double a = dx / dy;

            double xa;
            double phi = Math.PI * block.R / 180.0;
            if (Math.Abs(block.R - 90.0) < 0.000001) xa = x0;
            else if (Math.Abs(block.R + 90.0) < 0.000001) xa = x0;
            else xa = (a * xc + yc + Math.Tan(phi) * x0 - y0) / (Math.Tan(phi) + a);

            double ya = -a * (xa - xc) + yc;
            double l = Math.Sqrt(Math.Pow(xa - x0, 2) + Math.Pow(ya - y0, 2));

#if BoxDisplay  // Display Start, End and Anchor position for debug
            BoxView box1 = new BoxView() {
                TranslationX = xa,  TranslationY = ya,
                WidthRequest = l,   HeightRequest = 2,
                AnchorX = 0,        AnchorY = 0,
                Rotation = Math.Atan2(y0 - ya, x0 - xa) / Math.PI * 180.0,
                BackgroundColor = Color.Yellow
            };
            BoxView box2 = new BoxView() {
                TranslationX = xa,  TranslationY = ya,
                WidthRequest = l,   HeightRequest = 2,
                AnchorX = 0,        AnchorY = 0,
                Rotation = Math.Atan2(y - ya, x - xa) / Math.PI * 180.0,
                BackgroundColor = Color.Yellow
            };
            BoxView box3 = new BoxView() {
                TranslationX = x,   TranslationY = y,
                WidthRequest = 5,   HeightRequest = 5,
                BackgroundColor = Color.Red
            };

            Board.Children.Add(box1);
            Board.Children.Add(box2);
            Board.Children.Add(box3);
#endif

            if (xa - x0 > 0 && Math.Cos(phi) < 0) l *= -1;
            if (xa - x0 < 0 && Math.Cos(phi) > 0) l *= -1;
            double anchorx = l / block.W;

            double thetad = Math.PI - (Math.Atan2(dy, dx) - phi);
            double theta = (Math.PI - 2 * thetad) / Math.PI * 180.0;
            theta = theta % 360.0;
            if (theta > 180) theta -= 360;
            if (theta < -180) theta += 360;
            
            block.AnchorX = anchorx;
            block.AnchorY = 0;
            block.Body.Content.AnchorX = 0;
            block.Body.Content.AnchorY = 0;

            if (length == 0)
                length = (uint)(Math.Abs(Math.PI * theta / 180.0 * l) / 314.0 * 50 / Vb);

            double rotation;
            if (upright) rotation = -(block.R + theta);
            else rotation = 0;

            await Task.WhenAll(
                Rotate(block, theta, length),                
                block.Body.Content.RotateTo(rotation, length)
            );
        }
        

        // Rotate Block
        async Task Rotate(Block block, double rotation, uint length)
        {
            block.Body.AnchorX = block.AnchorX;
            block.Body.AnchorY = block.AnchorY;
            block.Body.Content.AnchorX = 0;
            block.Body.Content.AnchorY = 0;

            double theta0 = Math.Atan2(block.Body.AnchorY * block.H, block.Body.AnchorX * block.W);
            double theta = Math.PI * (rotation / 180.0);
            double phi = Math.PI * block.R / 180.0;
            double l = Math.Sqrt(Math.Pow(block.Body.AnchorX * block.W, 2)
                                 + Math.Pow(block.Body.AnchorY * block.H, 2));
            double dx = l * (Math.Cos(theta0 + phi) - Math.Cos(theta0));
            double dy = l * (Math.Sin(theta0 + phi) - Math.Sin(theta0));

            block.X += dx; block.Y += dy;
            await Task.WhenAll(
                block.Body.RelRotateTo(rotation, length),
                block.Body.Content.RotateTo(0, length)
            );
            block.X -= dx; block.Y -= dy;

            block.Body.AnchorX = 0;
            block.Body.AnchorY = 0;
            block.X += l * (Math.Cos(theta0 + phi) - Math.Cos(theta + theta0 + phi));
            block.Y += l * (Math.Sin(theta0 + phi) - Math.Sin(theta + theta0 + phi));

            block.R += rotation;
        }

    }
}
