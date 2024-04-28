using System;
using System.Collections.Generic;
using System.Text;

namespace Bounce_Companion
{
    internal class HandleChallenges
    {
        public MainWindow main;
        public HandleChallenges(MainWindow Main)
        {
            main = Main;
        }

        public void ChallengeComplete(string type, string map, string locationOrNumber)
        {
            if (type == "consec")
            {
                switch (int.Parse(locationOrNumber))
                {
                    case 2:
                        {
                            break;
                        };
                }
            }
            else if (type == "locational")
            { 
            
            }
        
        }
    }

    
}
