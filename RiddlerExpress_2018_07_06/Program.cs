using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Diagnostics;
using QuickGraph;
using QuickGraph.Algorithms.Observers;
using QuickGraph.Algorithms.ShortestPath;
using System.Drawing.Imaging;

namespace RiddlerExpress_2018_07_06
{
    class Program
    {
        private static int numEastWest = 61;
        private static int numNorthSouth = 21;
        private static string startVertex = "20F";

        static void Main(string[] args)
        {
            var graph = new AdjacencyGraph<string, Edge<string>>(false);
            var edgeCost = new Dictionary<Edge<string>, double>();

            //Add all vertices
            for (int eastWest = 0; eastWest < numEastWest; eastWest++)
            {
                for (int northSouth = 0; northSouth < numNorthSouth; northSouth++)
                {
                    string cornerName = ToCornerName(eastWest, northSouth);
                    Trace.WriteLine("Adding vertex: " + cornerName);

                    graph.AddVertex(cornerName);
                }
            }

            // Create the edges
            for (int eastWest = 0; eastWest < numEastWest; eastWest++)
            {
                for (int northSouth = 0; northSouth < numNorthSouth; northSouth++)
                {
                    var cornerName = ToCornerName(eastWest, northSouth);

                    AddEdge(graph, edgeCost, cornerName, ToCornerName(eastWest, northSouth - 1));
                    AddEdge(graph, edgeCost, cornerName, ToCornerName(eastWest, northSouth + 1));
                    AddEdge(graph, edgeCost, cornerName, ToCornerName(eastWest - 1, northSouth));
                    AddEdge(graph, edgeCost, cornerName, ToCornerName(eastWest + 1, northSouth));
                }
            }

            Func<Edge<string>, double> getWeight = edge => edgeCost[edge];

            // We want to use Dijkstra on this graph
            var dijkstra = new DijkstraShortestPathAlgorithm<string, Edge<string>>(graph, getWeight);

            // attach a distance observer to give us the shortest path distances
            var distObserver = new VertexDistanceRecorderObserver<string, Edge<string>>(getWeight);
            distObserver.Attach(dijkstra);

            // Attach a Vertex Predecessor Recorder Observer to give us the paths
            VertexPredecessorRecorderObserver<string, Edge<string>> predecessorObserver = new VertexPredecessorRecorderObserver<string, Edge<string>>();
            predecessorObserver.Attach(dijkstra);

            // Run the algorithm from the starting point
            dijkstra.Compute(startVertex);

            foreach (KeyValuePair<string, double> kvp in distObserver.Distances)
                Log(string.Format("Distance from {0} to node {1} is {2}", startVertex, kvp.Key, kvp.Value));

            foreach (KeyValuePair<string, Edge<string>> kvp in predecessorObserver.VertexPredecessors)
                Log(string.Format("If you want to get to {0} you have to enter through the in edge {1}", kvp.Key, kvp.Value));

            //Get those paths that travel at least once in the ultra path.
            var ultraEdgePaths = new List<Edge<string>>();
            foreach (var edge in predecessorObserver.VertexPredecessors.Values)
            {
                if (edge.Source.Contains("U") && edge.Target.Contains("U"))
                {
                    ultraEdgePaths.Add(edge);
                }
            }

            var destinationsTravellingUltra = GetUltraPaths(predecessorObserver);

            Bitmap bitmap = GenerateBitmap(destinationsTravellingUltra, distObserver);

            var filename = "C:/temp/riddle.png";
            bitmap.Save(filename, ImageFormat.Png);

            Process.Start(@"firefox.exe", "file://" + filename);

            destinationsTravellingUltra.Sort();
            foreach (var dest in destinationsTravellingUltra)
                Log("Destination travelling ultra: " + dest);

            var ultraProjection = from s in destinationsTravellingUltra
                      select new Tuple<int, string>(int.Parse(s.Substring(0, s.Length - 1)), s );

            var northCorners = from t in ultraProjection
                               where t.Item1 < 20
                               group t by t.Item2.Substring(t.Item2.Length-1) into g
                               orderby g.Key
                               select g.Max().Item2;

            var southCorners = from t in ultraProjection
                               where t.Item1 > 20
                               group t by t.Item2.Substring(t.Item2.Length - 1) into g
                               orderby g.Key descending
                               select g.Min().Item2;

            Log("All blocks north of and to the east of (including): " + string.Join(",", northCorners));
            Log("All blocks south of and to the east of (including): " + string.Join(",", southCorners));

        }

        private static void Log(string msg)
        {
            Console.WriteLine(msg);
            Trace.WriteLine(msg);
        }

        private static Bitmap GenerateBitmap(List<string> destinationsTravellingUltra, VertexDistanceRecorderObserver<string, Edge<string>> distObserver)
        {
            int bitmapSize = 2000;
            var bitmap = new Bitmap(bitmapSize, bitmapSize);
            var graphics = Graphics.FromImage(bitmap);

            graphics.FillRectangle(new SolidBrush(Color.White), 0, 0, bitmapSize, bitmapSize);

            var blackPen = new Pen(Color.Black);
            var greenPen = new Pen(Color.Green);
            var bluePen = new Pen(Color.Blue);

            var font = new Font("Arial", 8);
            var greenBrush = new SolidBrush(Color.Green);
            var blackBrush = new SolidBrush(Color.Black);
            var blueBrush = new SolidBrush(Color.Blue);

            int border = 50;

            for (int eastWest = 0; eastWest < numEastWest; eastWest++)
            {
                int hX1 = border;
                int hY1 = border + (((bitmapSize - (2 * border)) / (numEastWest - 1)) * eastWest);
                int hX2 = bitmapSize - border;
                int hY2 = hY1;

                graphics.DrawLine(blackPen, hX1, hY1, hX2, hY2);

                for (int northSouth = 0; northSouth < numNorthSouth; northSouth++)
                {
                    int vX1 = border + (((bitmapSize - (2 * border)) / (numNorthSouth - 1)) * northSouth);
                    int vY1 = border;
                    int vX2 = vX1;
                    int vY2 = bitmapSize - border;

                    Pen pen;
                    if (northSouth == ('U' - 'A'))
                        pen = greenPen;
                    else
                        pen = blackPen;

                    graphics.DrawLine(pen, vX1, vY1, vX2, vY2);

                    var cornerName = ToCornerName(eastWest, northSouth);
                    var distance = Math.Abs(eastWest - 19) + Math.Abs(northSouth - 5);
                    var cost = distObserver.Distances[cornerName];

                    SolidBrush brush;
                    if (destinationsTravellingUltra.Contains(cornerName))
                        brush = greenBrush;
                    else if (cornerName == startVertex)
                    {
                        brush = blueBrush;

                        graphics.DrawEllipse(bluePen, vX1 - 20, hY1 - 20, 40, 40);
                    }
                    else
                        brush = blackBrush;

                    graphics.DrawString(cornerName, font, brush, vX1, hY1);
                    graphics.DrawString(string.Format("{0}:{1}", distance * 18, cost), font, brush, vX1, hY1+10);

                    //straight cost vs ultra cost
                }
            }

            return bitmap;
        }

        private static List<string> GetUltraPaths(VertexPredecessorRecorderObserver<string, Edge<string>> predecessorObserver)
        {
            var destinationsTravellingUltra = new List<string>();

            foreach (var cornerName in predecessorObserver.VertexPredecessors.Keys)
            {
                var currentVertex = cornerName;

                while (currentVertex != startVertex)
                {
                    var edge = predecessorObserver.VertexPredecessors[currentVertex];
                    if (edge.Source.Contains("U") && edge.Target.Contains("U"))
                    {
                        destinationsTravellingUltra.Add(cornerName);
                        break;
                    }

                    currentVertex = edge.Source;
                }
            }

            return destinationsTravellingUltra;
        }

        private static void AddEdge(AdjacencyGraph<string, Edge<string>> graph, Dictionary<Edge<string>, double> edgeCost, string cornerName1, string cornerName2)
        {
            if (cornerName1 != null && cornerName2 != null)
            {
                if (!graph.TryGetEdge(cornerName1, cornerName2, out Edge<string> edge))
                {
                    var c1_c2 = new Edge<string>(cornerName1, cornerName2);
                    graph.AddEdge(c1_c2);

                    int cost; // time = distance / speed. Measure in seconds.
                    if (cornerName1.Contains("U") && cornerName2.Contains("U"))
                        cost = 2;
                    else
                        cost = 18;

                    edgeCost.Add(c1_c2, cost);
                }
                else
                {
                    int i = 0; //If this happens it's already added. 
                }
            }
        }

        private static string ToCornerName(int eastWest, int northSouth)
        {
            string ret = null;

            if (eastWest >= 0 &&
                eastWest < numEastWest &&
                northSouth >= 0 &&
                northSouth < numNorthSouth)
            {
                ret = (eastWest + 1).ToString() + ((char)('A' + northSouth)).ToString();
            }

            return ret;
        }
    }
}
