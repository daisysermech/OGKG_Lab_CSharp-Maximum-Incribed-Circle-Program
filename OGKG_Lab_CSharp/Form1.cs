using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CS_Boost_Wrapper;
using KdTree;
using KdTree.Math;
using SharpBoostVoronoi.Output;

namespace OGKG_Lab_CSharp
{
    public partial class Form1 : Form
    {
        List<PointF> points = new List<PointF>();
        Graphics G;
        bool Fig_Comp = false;
        public Form1()
        {
            InitializeComponent();
            G = CreateGraphics();

            comboBox1.Items.Add("Star 1");
            comboBox1.Items.Add("Star 2");
            comboBox1.Items.Add("Star 3");
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (!Fig_Comp)
                switch (e.Button)
                {
                    case MouseButtons.Left:
                        points.Add(new PointF(e.X, e.Y));
                        if (points.Count > 1)
                            G.DrawLine(Pens.Black, points[points.Count - 2], points[points.Count - 1]);
                        break;

                    case MouseButtons.Right:
                        if (points.Count > 2)
                        {
                            G.DrawLine(Pens.Black, points[points.Count - 1], points[0]);
                            Fig_Comp = true;
                        }
                        break;
                }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (points.Count < 3 || !Fig_Comp)
            {
                MessageBox.Show("At least 3 points should form a figure. Figure should be completed (right-click).","Error.",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }
            BoostVoronoi voronoi = new BoostVoronoi();
            for (int i = 0; i < points.Count; i++)
                voronoi.AddSegment(points[i].X, points[i].Y, points[(i + 1) % points.Count].X, points[(i + 1) % points.Count].Y);
            voronoi.Construct();

            List<long> inside_vertices = new List<long>();
            List<long> inside2 = new List<long>();
            for (int i = 0; i < voronoi.CountVertices; i++)
            {
                if (IsPointInPolygon(points, voronoi.GetVertex(i)) && !points.Contains(new PointF((float)voronoi.GetVertex(i).X, (float)voronoi.GetVertex(i).Y)))
                    inside_vertices.Add(i);
                else
                    if (points.Contains(new PointF((float)voronoi.GetVertex(i).X, (float)voronoi.GetVertex(i).Y)))
                    inside2.Add(i);
            }
            inside2.AddRange(inside_vertices);
            if (checkBox1.Checked)
            for (int i = 0; i < voronoi.CountEdges; i++)
            {
                if (!voronoi.GetEdge(i).IsFinite)
                    continue;
                if (inside2.Contains(voronoi.GetEdge(i).Start) && inside2.Contains(voronoi.GetEdge(i).End))
                    G.DrawLine(Pens.Red, (float)voronoi.GetVertex(voronoi.GetEdge(i).Start).X,
                        (float)voronoi.GetVertex(voronoi.GetEdge(i).Start).Y,
                        (float)voronoi.GetVertex(voronoi.GetEdge(i).End).X,
                        (float)voronoi.GetVertex(voronoi.GetEdge(i).End).Y);
            }

            KdTree<float, int> tree = new KdTree<float, int>(2, new FloatMath());
            for (int i = 0; i < points.Count(); i++)
                tree.Add(new[] { points[i].X, points[i].Y }, i);
            var vertexes = new List<Tuple<Vertex, float>>();
            foreach (var i in inside_vertices)
            {
                var node = tree.GetNearestNeighbours(new[] { (float)voronoi.GetVertex(i).X, (float)voronoi.GetVertex(i).Y }, 1)[0];
                int index = node.Value;
                vertexes.Add(new Tuple<Vertex, float>(voronoi.GetVertex(i), Math.Min(
                    FindDistanceToSegment(voronoi.GetVertex(i), points[(index - 1 + points.Count) % points.Count], points[index]),
                    FindDistanceToSegment(voronoi.GetVertex(i), points[index], points[(index + 1) % points.Count]))));
            }

            Tuple<Vertex, float> ans = vertexes[0];
            for (int i = 1; i < vertexes.Count; i++)
                if (vertexes[i].Item2 > ans.Item2)
                    ans = vertexes[i];

            PointF center = new PointF((float)ans.Item1.X, (float)ans.Item1.Y);
            G.DrawEllipse(Pens.Blue, center.X - ans.Item2, center.Y - ans.Item2, ans.Item2 * 2, ans.Item2 * 2);

            label1.Visible = true;
            label1.Text = "Center of circle: (" + center.X + " ; " + center.Y + ")\nRadius is: " + ans.Item2;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            G.Clear(DefaultBackColor);
            points.Clear();
            Fig_Comp = false;
            label1.Visible = false;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {

        }
        private float FindDistanceToSegment(Vertex pt, PointF p1, PointF p2)
        {
            PointF closest;
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            if ((dx == 0) && (dy == 0))
            {
                closest = p1;
                dx = (float)(pt.X - p1.X);
                dy = (float)(pt.Y - p1.Y);
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }

            float t = (float)(((pt.X - p1.X) * dx + (pt.Y - p1.Y) * dy) /
                (dx * dx + dy * dy));

            if (t < 0)
            {
                closest = new PointF(p1.X, p1.Y);
                dx = (float)(pt.X - p1.X);
                dy = (float)(pt.Y - p1.Y);
            }
            else if (t > 1)
            {
                closest = new PointF(p2.X, p2.Y);
                dx = (float)(pt.X - p2.X);
                dy = (float)(pt.Y - p2.Y);
            }
            else
            {
                closest = new PointF(p1.X + t * dx, p1.Y + t * dy);
                dx = (float)(pt.X - closest.X);
                dy = (float)(pt.Y - closest.Y);
            }

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static bool IsPointInPolygon(List<PointF> polygon, Vertex testPoint)
        {
            bool result = false;
            int j = polygon.Count() - 1;
            for (int i = 0; i < polygon.Count(); i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
                        result = !result;
                j = i;
            }
            return result;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Calculates a maximum inscribed circle in user-input figure. To enter " +
                "a figure left-click stimulaneosly to create points. Right-click to stop.");
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            button2_Click(sender, e);
            if (comboBox1.SelectedItem.Equals("Star 1"))
            {
                points.Add(new PointF(310, 171));
                points.Add(new PointF(348, 73));
                points.Add(new PointF(378, 160));
                points.Add(new PointF(487, 170));
                points.Add(new PointF(392, 205));
                points.Add(new PointF(442, 291));
                points.Add(new PointF(349, 222));
                points.Add(new PointF(286, 280));
                points.Add(new PointF(305, 208));
                points.Add(new PointF(221, 172));
            }
            if (comboBox1.SelectedItem.Equals("Star 2"))
            {
                points.Add(new PointF(313, 109));
                points.Add(new PointF(329, 28));
                points.Add(new PointF(359, 111));
                points.Add(new PointF(441, 47));
                points.Add(new PointF(401, 106));
                points.Add(new PointF(556, 138));
                points.Add(new PointF(408, 138));
                points.Add(new PointF(523, 233));
                points.Add(new PointF(410, 189));
                points.Add(new PointF(481, 341));
                points.Add(new PointF(380, 192));
                points.Add(new PointF(378, 325));
                points.Add(new PointF(310, 342));
                points.Add(new PointF(331, 203));
                points.Add(new PointF(223, 321));
                points.Add(new PointF(288, 217));
                points.Add(new PointF(133, 223));
                points.Add(new PointF(271, 172));
                points.Add(new PointF(156, 116));
            }
            if (comboBox1.SelectedItem.Equals("Star 3"))
            {
                points.Add(new PointF(223, 235));
                points.Add(new PointF(37, 352));
                points.Add(new PointF(201, 294));
                points.Add(new PointF(238, 326));
                points.Add(new PointF(185, 394));
                points.Add(new PointF(293, 346));
                points.Add(new PointF(334, 395));
                points.Add(new PointF(360, 320));
                points.Add(new PointF(503, 359));
                points.Add(new PointF(375, 270));
                points.Add(new PointF(649, 287));
                points.Add(new PointF(415, 209));
                points.Add(new PointF(592, 151));
                points.Add(new PointF(387, 149));
                points.Add(new PointF(437, 53));
                points.Add(new PointF(336, 137));
                points.Add(new PointF(314, 65));
                points.Add(new PointF(306, 140));
                points.Add(new PointF(141, 102));
                points.Add(new PointF(272, 155));
                points.Add(new PointF(77, 152));
            }
            Fig_Comp = true;
            for(int i=0;i<points.Count;i++)
                G.DrawLine(Pens.Black, points[i], points[(i+1)%points.Count]);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
