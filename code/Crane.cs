using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;

namespace OptPacking
{
    public partial class MainPage : ContentPage
    {
        bool CraneStart = false;    // Start trigger for the cranes

        int NumCranes = 5;          // The Number of Cranes
        int NumSections = 6;        // The Number of Sections
        double Vc = 0.1;            // Average Velocity of Cranes
        double CraneWidth = 10.0;   // Crane width

        // Crane class
        class Crane
        {
            private ContentView body = new ContentView()
            {
                WidthRequest = 10,
                HeightRequest = 50,
                TranslationX = 300,
                TranslationY = 0,
                AnchorX = 0.5,
                AnchorY = 1.5,                
                Content = new AbsoluteLayout
                {
                    WidthRequest = 10,
                    HeightRequest = 200,
                    BackgroundColor = Color.Yellow
                }
            };
            public ContentView Body { get { return this.body; } }
            public int Section { get; set; }
            public Crane Ahead { get; set; }
        }
        List<Crane> Cranes = new List<Crane>();

        BoxView Gear1 = new BoxView
        {
            WidthRequest = 3,   HeightRequest = 40,
            TranslationX = 105, TranslationY = 54,
            AnchorX = 0.5,      AnchorY = 0.5,
            BackgroundColor = Color.Yellow
        };
        BoxView Gear2 = new BoxView
        {
            WidthRequest = 3,   HeightRequest = 40,
            TranslationX = 305, TranslationY = 54,
            AnchorX = 0.5,      AnchorY = 0.5,
            BackgroundColor = Color.Yellow
        };


        // Create and Initialize Cranes
        void InitCranes()
        {
            CraneStart = false;

            for (int i = 0; i < NumCranes; i++)
            {
                Crane crane = new Crane();
                Cranes.Add(crane);
                Board.Children.Add(crane.Body);
            }

            foreach(Crane crane in Cranes)
            {
                int index = Cranes.IndexOf(crane);

                if (index == 0) crane.Ahead = Cranes[NumCranes - 1];
                else crane.Ahead = Cranes[index - 1];
            }


            Cranes[0].Section = 5;

            Cranes[1].Body.TranslationX = 200;
            Cranes[1].Section = 4;

            Cranes[2].Body.TranslationX = 100;
            Cranes[2].Section = 3;

            Cranes[3].Body.TranslationX = 100;
            Cranes[3].Body.Rotation = 180;
            Cranes[3].Section = 2;

            Cranes[4].Body.TranslationX = 200;
            Cranes[4].Body.Rotation = 180;
            Cranes[4].Section = 1;

            Board.Children.Add(Gear1);
            Board.Children.Add(Gear2);
        }


        // Operate each Crane
        async Task OperateCrane(Crane crane)
        {
            CraneStart = true;

            while(CraneStart)
            {
                for (int i = 0; i < NumSections; i++)
                {                    
                    while (true)
                    {
                        bool MoveNextSection = (crane.Ahead.Section != crane.Section + 1);
                        if (crane.Section == NumCranes)
                            MoveNextSection = ((crane.Ahead.Section != 0) && (crane.Ahead.Section != NumCranes));

                        if (MoveNextSection) break;
                    }

                    crane.Section++;
                    if (crane.Section > NumCranes)
                    {
                        ServeBlock = true;
                        crane.Section = 0;                        
                    }

                    await MoveCrane(crane);

                }
            }
        }


        // Move each Crane in each Section
        async Task MoveCrane(Crane crane)
        {            
            switch (crane.Section)
            {
                case 0:
                case 3:                    
                    await crane.Body.RelRotateTo(180, (uint)(100.0 / Vc));
                    break;

                case 1:
                case 4:                    
                    await crane.Body.TranslateTo(200, 0, (uint)(100.0 / Vc));
                    break;

                case 2:                    
                    await crane.Body.TranslateTo(100, 0, (uint)(100.0 / Vc));
                    break;

                case 5:                    
                    await crane.Body.TranslateTo(300, 0, (uint)(100.0 / Vc));
                    break;
            }
        }


        // Rotate the Crane Gears
        async Task RotateGear()
        {
            while (CraneStart)
            {
                await Task.WhenAll(
                    Gear1.RelRotateTo(360, 2000),
                    Gear2.RelRotateTo(360, 2000)
                );
            }
        }


    }
}
