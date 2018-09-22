using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace OptPacking
{
    public partial class MainPage : ContentPage
    {
        double Area = 0.0;          // Total Area of the Packed Blocks
        double BestHeight = 0.0;    // The Lowest Height in the Local Search
        double PackRatio = 0.0;     // Filling rate of the Packed Blocks
        int Iteration = 3000;
        double maxTemp = 5.0;
        double minTemp = 0.5;

        // No-Fit Polygon class for Bottom-Left algorithm
        class NFP
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double W { get; set; }
            public double H { get; set; }
        }
        List<NFP> NFPs = new List<NFP>();

        List<Block> PackedBlocks = new List<Block>();   // Packed Blocks Lists
        List<Block> BestBlocks = new List<Block>();     // Best Packed Blocks in Local Search

        // Level class for 'X'FDH algorithms
        class Level
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double H { get; set; }
            public double ResidualX { get { return X0 + ContainerWidth - X; } }
            public double NextY { get { return Y - H; } }
        }
        List<Level> Levels = new List<Level>();



        // X-Fit Decreasing Height algorityms
        double XFDH(string x, ref List<Block> blocks)
        {
            blocks = SortBlocks(Blocks, "height");
            Levels.Clear();

            Level PackLevel = new Level()
            {
                X = X0,
                Y = Y0,
                H = blocks[0].H
            };
            Levels.Add(PackLevel);
            double height = PackLevel.H;

            foreach (Block block in blocks)
            {
                bool Packed = false;
                block.Body.Content.BackgroundColor = Color.Aqua;

                switch (x)
                {
                    case "NFDH":    // Near-Fit Decreaseing Height
                        if (block.W < PackLevel.ResidualX) Packed = true;
                        break;

                    case "FFDH":    // Fast-Fit Decreasing Height
                        foreach (Level level in Levels)
                            if (block.W < level.ResidualX)
                            {
                                Packed = true;
                                PackLevel = level;
                                break;
                            }
                        break;

                    case "BFDH":    // Best-Fit Decreasing Height　
                        List<Level> SortedLevels
                            = new List<Level>(Levels.OrderBy(n => n.ResidualX));
                        foreach (Level level in SortedLevels)
                            if (block.W < level.ResidualX)
                            {
                                Packed = true;
                                PackLevel = level;
                                break;
                            }
                        break;
                }


                // if the block can not be packed in the last level, 
                // create a new level
                if (!Packed)
                {
                    PackLevel = new Level()
                    {
                        X = X0,
                        Y = Levels.Last().NextY,
                        H = block.H
                    };
                    Levels.Add(PackLevel);
                    height += PackLevel.H;
                }

                block.Xg = PackLevel.X;
                block.Yg = PackLevel.Y;

                // Shift the next block position
                PackLevel.X += block.W;

                // If the block is packed at the left end, 
                // the Level heigh is equal to its block height
                if (block.Xg < X0 + 1.0) PackLevel.H = block.H;
            }

            return height;

        }



        // Bottom-Left algorithm
        double BottomLeft(ref List<Block> blocks, string sort = "none")
        {
            int x = 0; int y = 0;
            bool FindBL = true;
            double height = 0;

            if (sort != "none") blocks = SortBlocks(blocks, sort);

            PackedBlocks.Clear();

            foreach (Block block in blocks)
            {
                // Make NFPs of all PackedBlocks
                NFPs.Clear();
                foreach (Block packedblock in PackedBlocks)
                {
                    NFP nfp = new NFP
                    {
                        X = packedblock.Xg - block.W,
                        Y = packedblock.Yg - packedblock.H,
                        W = packedblock.W + block.W,
                        H = packedblock.H + block.H
                    };
                    NFPs.Add(nfp);
                }


                // Scan the Container to find the Bottom-Left Point
                for (y = (int)Y0; y > 0; y--)
                {
                    for (x = (int)X0; x < (int)(X0 + ContainerWidth - block.W); x++)
                    {
                        FindBL = true;
                        foreach (NFP nfp in NFPs)
                        {
                            // Scanning point (x, y) is in any NFP, or not?
                            if (x > nfp.X && x < nfp.X + nfp.W
                             && y > nfp.Y && y < nfp.Y + nfp.H)
                            {
                                FindBL = false;
                                x = (int)(nfp.X + nfp.W);   // Skip to the right end of this NFP
                                break;
                            }
                        }
                        // if (x,y) is not in any NFPs, the point is BL.
                        if (FindBL) break;
                    }
                    if (FindBL) break;
                }

                block.Xg = (double)x;
                block.Yg = (double)y;

                if (Y0 - (block.Yg - block.H) > height)
                    height = Y0 - (block.Yg - block.H);

                PackedBlocks.Add(block);
            }

            return height;
        }



        // Local Searach to find the best packed solution
        async void LocalSearch(object sender, EventArgs e)
        {
            int i = 1;
            BestHeight = 500.0;
            List<Block> blocks = new List<Block>(SortBlocks(Blocks, "Area"));

            while (i < Iteration)
            {
                await SimulatedAnnealing(blocks, i);
                
                await MoveAllBlocks(BestBlocks);
                Label8.Text = string.Format(" iterations: # {0}", i + 1);

                i += i;
                Label2.Text += i.ToString() + " ";
            }
            Label8.Text = string.Format(" iterations: # {0}END", i + 1);

        }


        // Simulated Annealing method for Local Search
        async Task SimulatedAnnealing(List<Block> blocks, int iteration)
        {
            List<Block> NeighbourBlocks = new List<Block>();
            int i = 0;
            int j = 0; int k = 0;
            double height = 0;
            double heightNeighbour = 0;
            double p = 0;
            double t = 0;

            // Local Search Loop
            for (i = iteration; i < iteration*2; i++)
            {
                int index1 = rnd.Next(NumBlocks - 1);
                int index2 = rnd.Next(NumBlocks - 1);

                NeighbourBlocks = new List<Block>(blocks);
                /*
                // Create a neighbouring blocks of the current blocks by
                //    moving a randomly chosen blocks to randomly chosen position
                Block dummy = NeighbourBlocks[index1];
                NeighbourBlocks.Remove(NeighbourBlocks[index1]);
                NeighbourBlocks.Insert(index2, dummy);
                */
                // Create a neighbouring blocks of the current blocks
                //    by exchanging randomly chosen two blocks
                Block dummy1 = NeighbourBlocks[index1];
                Block dummy2 = NeighbourBlocks[index2];
                NeighbourBlocks.Remove(NeighbourBlocks[index1]);
                NeighbourBlocks.Insert(index1, dummy2);
                NeighbourBlocks.Remove(NeighbourBlocks[index2]);
                NeighbourBlocks.Insert(index2, dummy1);


                // Calculate the energy of the neighbouring and the original blocks
                heightNeighbour = BottomLeft(ref NeighbourBlocks);
                height          = BottomLeft(ref blocks);

                if (height > heightNeighbour)
                {
                    blocks = new List<Block>(NeighbourBlocks);
                    if (BestHeight > heightNeighbour)
                    {
                        BestHeight = heightNeighbour;
                        Label3.Text += BestHeight.ToString() + " ";
                        BestBlocks = new List<Block>(NeighbourBlocks);                        
                    }
                    j++;
                }
                else// if(heightNeighbour - height < 20.0)
                {
                    // Calculate Acceptance Probability for Simulated Annealing
                    t = maxTemp * Math.Pow(minTemp / maxTemp, (double)i / (double)Iteration);
                    p = Math.Exp((height - heightNeighbour) / t);
                    if (rnd.NextDouble() < p)
                    {
                        blocks = new List<Block>(NeighbourBlocks);
                        k++;
                    }
                }
            }


            await Task.Delay(1);
            Label1.Text = string.Format("Iteration: {0}   Temperature:  {1:0.00}", i, t);
            //Label2.Text = string.Format("Find Better Sol: # {0}   Acceptance Prob: # {1}", j, k);
            
        }


        // Move All Blocks to the better solution
        async Task MoveAllBlocks(List<Block> blocks)
        {
            double height = BottomLeft(ref blocks);

            var tasks = new List<Task>();
            foreach (Block block in blocks)
                tasks.Add(block.Body.TranslateTo(block.Xg, block.Yg - block.H, 700));            

            await Task.WhenAll(tasks);

            // Fit the Container size to the Packed Blocks            
            PackRatio = ExpandContainer(height);
            Label6.Text = string.Format(" max height: {0} pixel", height);
            Label7.Text = string.Format(" pack ratio: {0:0.0} %", PackRatio);
        }



        // Sort the Blocks in some orders
        List<Block> SortBlocks(List<Block> blocks, string order)
        //                                       string order, bool rename = true)
        {

            switch(order)
            {
                // Sort the blocks in their Height order
                case "height":
                case "Height":
                    blocks = new List<Block>(blocks.OrderByDescending(n => n.H));
                    break;

                // Sort the blocks in their Area order
                case "area":
                case "Area":
                    blocks = new List<Block>(blocks.OrderByDescending(n => n.Area));
                    break;

                // Sort the blocks in their Height order
                case "width":
                case "Width":
                    blocks = new List<Block>(blocks.OrderByDescending(n => n.W));
                    break;

                // Sort the blocks in their X-position
                case "x":
                case "X":
                    blocks = new List<Block>(blocks.OrderByDescending(n => n.X));
                    break;

                // Sort the blocks in their Y-position
                case "-y":
                case "-Y":
                    blocks = new List<Block>(blocks.OrderBy(n => n.Y));
                    break;

                // Sort the blocks in their Height order
                case "original":
                case "Original":
                    blocks = new List<Block>(blocks.OrderByDescending(n => n.No));
                    break;

            }


            foreach (Block block in blocks)
            {
                int index = blocks.IndexOf(block);
                block.No = index + 1;
                block.Name = block.No.ToString();
                if (OperateSystemButton.Text != "Change")
                    block.X = index * 150 + 150;
            }

            return blocks;
        }


        // Expand the Container when the Packed Blocks are full
        double ExpandContainer(double height)
        {
            ContainerHeight = height;
            ContainerBox.TranslationY = Y0 - height;
            ContainerBox.HeightRequest = height;

            return Area / (ContainerWidth * ContainerHeight) * 100;
        }


    }
}
