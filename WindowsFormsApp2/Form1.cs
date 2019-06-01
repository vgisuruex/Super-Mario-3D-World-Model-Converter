using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Assimp;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using Newtonsoft.Json;
using SuperMario3DWorldModelConverter.Properties;
using Syroot.NintenTools.Bfres;
using Syroot.NintenTools.Bfres.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Material = Syroot.NintenTools.Bfres.Material;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        string objReady = "";
        Dictionary<string, string> Diffuse = new Dictionary<string, string>();
        Dictionary<string, string> Specular = new Dictionary<string, string>();
        Dictionary<string, string> Normal = new Dictionary<string, string>();
        Dictionary<string, string> MaterialList = new Dictionary<string, string>();
        int imageType = 0;
        HelixToolkitScene scene2 = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Clear the variables for second use
            objReady = "";
            Diffuse = new Dictionary<string, string>();
            Specular = new Dictionary<string, string>();
            Normal = new Dictionary<string, string>();
            MaterialList = new Dictionary<string, string>();
            imageType = 0;
            scene2 = null;
            //Clear the variables for second use


            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Blender Files | *.blend";
            if (ofd.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }
            List<int> VertexCounts = new List<int>();
            progressBar1.Value = 10;

            Importer importer = new Importer();
            scene2 = importer.Load(ofd.FileName);
            HelixToolkit.Wpf.SharpDX.Assimp.Exporter exporter = new HelixToolkit.Wpf.SharpDX.Assimp.Exporter();
            exporter.ExportToFile("Temp.dae", scene2.Root, "collada");
            string file = File.ReadAllText("Temp.dae");
            int LastLine = 0;
            foreach (SceneNode node in scene2.Root.Items)
            {
                string Node = "";
                string subbedString = file.Substring(LastLine);
                if (subbedString.Contains("<node id="))
                {
                    Node = subbedString.Substring(subbedString.IndexOf("<node id="), subbedString.IndexOf("</node>") + "</node>".Length - subbedString.IndexOf("<node id="));
                    if (!Node.Contains("<instance_geometry"))
                    {
                        file = file.Replace(Node, "");
                    }
                }
                else break;
                LastLine = subbedString.IndexOf("</node>") + "</node>".Length + LastLine - Node.Length;
            }
            Texture texture = new Texture();
            File.WriteAllText("Temp.dae", file);
            scene2 = importer.Load("Temp.dae");
            var lines = File.ReadLines("Temp.dae");
            progressBar1.Value = 20;
            string MaterialName = "EmptyName";
            string ItemName = "EmptyItem";
            MaterialList.Clear();
            foreach (string line in lines)
            {
                if (line.Contains("-positions-array\" count=\""))
                {
                    VertexCounts.Add(Convert.ToInt32(Regex.Match(line.Substring(line.IndexOf("-positions-array\" count=\"") + "-positions-array\" count=\"".Length, line.IndexOf("\">") - 59), @"\d+").Value) / 3);
                }
                else if (line.Contains("<init_from>"))
                {
                    if (line.Contains(@":\"))
                    {
                        if (imageType == 0)
                        {
                            Diffuse.Add(MaterialName, line.Substring(line.LastIndexOf(@"\") + 1, line.IndexOf(".") - line.LastIndexOf(@"\") - 1));
                        }
                        if (imageType == 1)
                        {
                            Specular.Add(MaterialName, line.Substring(line.LastIndexOf(@"\") + 1, line.IndexOf(".") - line.LastIndexOf(@"\") - 1));
                        }
                        if (imageType == 2)
                        {
                            Normal.Add(MaterialName, line.Substring(line.LastIndexOf(@"\") + 1, line.IndexOf(".") - line.LastIndexOf(@"\") - 1));
                        }
                    }
                }
                else if (line.Contains("<image id=\""))
                {
                    if (line.Contains("-diffuse"))
                    {
                        imageType = 0;
                        MaterialName = line.Substring(line.IndexOf("<image id=\"") + "<image id=\"".Length, line.IndexOf("-diffuse") - 3 - line.IndexOf("<image id=\"") - "-diffuse".Length);
                    }
                    else if (line.Contains("-normal"))
                    {
                        imageType = 2;
                        MaterialName = line.Substring(line.IndexOf("<image id=\"") + "<image id=\"".Length, line.IndexOf("-specular") - 3 - line.IndexOf("<image id=\"") - "-specular".Length);
                    }
                    else if (line.Contains("-specular"))
                    {
                        imageType = 1;
                        MaterialName = line.Substring(line.IndexOf("<image id=\"") + "<image id=\"".Length, line.IndexOf("-normal") - 3 - line.IndexOf("<image id=\"") - "-normal".Length);
                    }
                }
                else if (line.Contains("<node id=\""))
                {
                    ItemName = line.Substring(line.IndexOf("<node id=\"") + "<node id=\"".Length, line.IndexOf("\"  name=\"") - (line.IndexOf("<node id=\"") + "<node id=\"".Length));
                }
                else if (line.Contains("target=\""))
                {
                    MaterialList.Add(ItemName, line.Substring(line.IndexOf("target=\"#") + "target=\"#".Length, line.IndexOf("\">") - (line.IndexOf("target=\"#") + "target=\"#".Length)));
                }
            }
            string EmptyDae = Resources.Empty;
            string Obj = File.ReadAllText("Temp.dae");
            string Geometries = "<library_geometries>" + @"
";
            progressBar1.Value = 30;
            string librarygeometries = Obj.Substring(Obj.IndexOf("<library_geometries>"), Obj.LastIndexOf("</library_geometries>") - Obj.IndexOf("<library_geometries>") + "</library_geometries>".Length);
            int id = 0;
            foreach (SceneNode item in scene2.Root.Traverse())
            {
                if (item is MeshNode)
                {
                    if (librarygeometries.Contains("<geometry id="))
                    {
                        string Geo;
                        if (id == 0)
                        {
                            Geo = librarygeometries.Substring(librarygeometries.IndexOf("<geometry id="), librarygeometries.IndexOf("</geometry>") - 13);
                        }
                        else
                        {
                            Geo = librarygeometries.Substring(librarygeometries.IndexOf("<geometry id="), librarygeometries.IndexOf("</geometry>") - librarygeometries.IndexOf("<geometry id="));
                        }
                        if (!Geo.Contains(@"-color0"" name=""meshId"""))
                        {
                            string colorReference = Geo.Substring(0, Geo.LastIndexOf("/>") + 2) + @"
          <input offset=""0"" semantic=""COLOR"" source=""#meshId" + id + @"-color0"" set=""0"" />" + Geo.Substring(Geo.LastIndexOf("/>") + 2);

                            Geo = colorReference.Substring(0, colorReference.LastIndexOf("</source>") + "</source>".Length) + @"
        <source id=""meshId" + id + @"-color0"" name=""meshId" + id + @"-color0"">
          <float_array id=""meshId" + id + @"-color0-array"" count=""" + VertexCounts[id] * 3 + @"""> " + String.Concat(Enumerable.Repeat("1 ", VertexCounts[id] * 3)) + @" </float_array>
          <technique_common>
            <accessor count=""" + VertexCounts[id] * 3 + @""" offset=""0"" source=""#meshId" + id + @"-color0-array"" stride=""3"">
              <param name=""R"" type=""float"" />
              <param name=""G"" type=""float"" />
              <param name=""B"" type=""float"" />
            </accessor>
          </technique_common>
        </source>" + colorReference.Substring(colorReference.LastIndexOf("</source>") + "</source>".Length);
                        }
                        if (id == 0) Geometries = Geometries + Geo;
                        else Geometries = Geometries + Geo + @"</geometry>
";
                        if (id == 0)
                        {
                            librarygeometries = librarygeometries.Remove(librarygeometries.IndexOf("<geometry id="), librarygeometries.IndexOf("</geometry>") + "</geometry>".Length - librarygeometries.IndexOf("<geometry id="));
                        }
                        else
                        {
                            librarygeometries = librarygeometries.Remove(librarygeometries.IndexOf("<geometry id="), librarygeometries.IndexOf("</geometry>") + "</geometry>".Length - librarygeometries.IndexOf("<geometry id="));
                        }
                    }
                    id++;
                }
            }
            progressBar1.Value = 50;
            Geometries = Geometries + "</library_geometries>" + @"
";
            string objWithGeo = EmptyDae.Substring(0, 5957) + @"
  " + Geometries + @"
  " + EmptyDae.Substring(EmptyDae.IndexOf("<library_controllers>"));
            int loopnumber = 0;
            string objWithSkin = objWithGeo.Substring(0, objWithGeo.LastIndexOf("</library_controllers>"));
            progressBar1.Value = 60;
            foreach (SceneNode Item in scene2.Root.Items)
            {
                if (id > VertexCounts.Count)
                {
                    id--;
                }
                string controller = @"<controller id=""meshId0-skin"" name=""skinCluster0"">
      <skin source=""#meshId0"">
        <bind_shape_matrix>
          1 0 0 0
          0 1 0 0
          0 0 1 0
          0 0 0 1
        </bind_shape_matrix>
        <source id=""meshId0-skin-joints"" name=""meshId0-skin-joints"">
          <Name_array id=""meshId0-skin-joints-array"" count=""1"">EnterCatMarioStepB </Name_array>
          <technique_common>
            <accessor source=""#meshId0-skin-joints-array"" count=""1"" stride=""1"">
              <param name=""JOINT"" type=""Name""></param>
            </accessor>
          </technique_common>
        </source>
        <source id=""meshId0-skin-bind_poses"" name=""meshId0-skin-bind_poses"">
          <float_array id=""meshId0-skin-bind_poses-array"" count=""16""> 1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1 </float_array>
          <technique_common>
            <accessor count=""1"" offset=""0"" source=""#meshId0-skin-bind_poses-array"" stride=""16"">
              <param name=""TRANSFORM"" type=""float4x4"" />
            </accessor>
          </technique_common>
        </source>
        <source id=""meshId0-skin-weights"" name=""meshId0-skin-weights"">
          <float_array id=""meshId0-skin-weights-array"" count=""1654"">" + String.Concat(Enumerable.Repeat("1 ", VertexCounts[loopnumber])) + @" </float_array>
          <technique_common>
            <accessor count=" + "\"" + VertexCounts[loopnumber] + "\"" + @" offset =""0"" source=""#meshId0-skin-weights-array"" stride=""1"">
              <param name=""WEIGHT"" type=""float"" />
            </accessor>
          </technique_common>
        </source>
        <joints>
          <input semantic=""JOINT"" source=""#meshId0-skin-joints""></input>
          <input semantic=""INV_BIND_MATRIX"" source=""#meshId0-skin-bind_poses""></input>
        </joints>
        <vertex_weights count=" + "\"" + VertexCounts[loopnumber] + "\"" + @">
          <input semantic=""JOINT"" source=""#meshId0-skin-joints"" offset=""0""></input>
          <input semantic=""WEIGHT"" source=""#meshId0-skin-weights"" offset=""1""></input>
          <vcount>" + String.Concat(Enumerable.Repeat("1 ", VertexCounts[loopnumber])) + " </vcount>";
                string ControllerFixed = controller.Replace("skinCluster0", "skinCluster" + loopnumber);
                string vertexWeights = MakeZeroVertexWeights(VertexCounts[loopnumber]);
                objWithSkin = objWithSkin + @"
" + ControllerFixed.Replace("meshId0", "meshId" + loopnumber) + @"
" + vertexWeights;
                loopnumber++;
            }
            progressBar1.Value = 80;
            objWithSkin = objWithSkin + objWithGeo.Substring(objWithGeo.LastIndexOf("</library_controllers>"));
            string Object = objWithSkin.Substring(0, objWithSkin.LastIndexOf(@"<visual_scene id=""Bfres"" name=""Bfres"">"));
            Object = Object + Obj.Substring(Obj.IndexOf("<visual_scene id="), Obj.IndexOf("</visual_scene>") - Obj.IndexOf("<visual_scene id=")).Replace("&lt;BlenderRoot&gt;", "Bfres");
            Object = Object.Substring(0, Object.IndexOf(@"<visual_scene id=""Bfres"" name=""Bfres"">") + @"<visual_scene id=""Bfres"" name=""Bfres"">".Length) + @"
      " + @"<node id=""EmptyBoneName"" sid=""EmptyBoneName"" name=""EmptyBoneName"" type=""JOINT"">
        <matrix sid=""matrix"">1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1</matrix>
      </node>" + Object.Substring(Object.IndexOf(@"<visual_scene id=""Bfres"" name=""Bfres"">") + @"<visual_scene id=""Bfres"" name=""Bfres"">".Length);
            int loopingNumber = 0;
            foreach (SceneNode node in scene2.Root.Items)
            {
                Object = Object.Replace(@"<instance_geometry url=""#meshId" + loopingNumber + @""">", @"<instance_controller url=""#meshId" + loopingNumber + @"-skin"">
          <skeleton>#EmptyBoneName</skeleton>");
                Object = Object.Replace("</instance_geometry>", "</instance_controller>");
                loopingNumber++;
            }
            Debug.WriteLine(Object);
            progressBar1.Value = 90;
            progressBar1.Value = 100;
            objReady = Object + objWithSkin.Substring(objWithSkin.LastIndexOf("</visual_scene>"));
            if (textBox1.Text != "")
            {
                objReady = objReady.Replace("EmptyBoneName", textBox1.Text);
                objReady = objReady.Replace("Bfres", textBox1.Text);
            }
            File.Delete("Temp.dae");
            MessageBox.Show("Successfuly imported from " + ofd.FileName + "!");

            progressBar1.Value = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
        public string MakeZeroVertexWeights(int vertexNumber)
        {
            int countedNumber = 0;
            string Weights = "<v>";
            if (countedNumber != vertexNumber)
            {
                for (int i = 0; i < vertexNumber; i++)
                {
                    Weights = Weights + 0 + " " + i + " ";
                    countedNumber = i;
                }
            }
            Weights = Weights + @"</v>
        </vertex_weights>
      </skin>
</controller>
";
            return Weights;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (objReady != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Select a location to save the dae file";
                sfd.Filter = "Collada dae files | *.dae";
                if (textBox1.Text != "")
                {
                    sfd.FileName = sfd.FileName + textBox1.Text + ".dae";
                }
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, objReady);
                    MessageBox.Show("Saved to " + sfd.FileName + "!");
                }
            }
            else MessageBox.Show("No file opened", "select a file first", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }


        private void button3_Click(object sender, EventArgs e)
        {
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (MaterialList.Count != 0)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Bfres File | *.bfres";
                ofd.Title = "Select the bfres you imported the model into";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    ResFile resFile = new ResFile(ofd.FileName);
                    ByteArrayToFile("Material.bfres", Resources.Material);
                    ResFile matres = new ResFile("Material.bfres");
                    int loopNumber = 0;
                    int loop = 0;
                    foreach (Model model in resFile.Models.Values)
                    {
                        foreach (string str in MaterialList.Values)
                        {
                            resFile.Models[loop].Materials[loopNumber].Flags = matres.Models[0].Materials[1].Flags;
                            resFile.Models[loop].Materials[loopNumber].RenderInfos = matres.Models[0].Materials[1].RenderInfos;
                            resFile.Models[loop].Materials[loopNumber].RenderState = matres.Models[0].Materials[1].RenderState;
                            resFile.Models[loop].Materials[loopNumber].Samplers = matres.Models[0].Materials[1].Samplers;
                            resFile.Models[loop].Materials[loopNumber].ShaderAssign = matres.Models[0].Materials[1].ShaderAssign;
                            resFile.Models[loop].Materials[loopNumber].ShaderParamData = matres.Models[0].Materials[1].ShaderParamData;
                            resFile.Models[loop].Materials[loopNumber].ShaderParams = matres.Models[0].Materials[1].ShaderParams;
                            resFile.Models[loop].Materials[loopNumber].UserData = matres.Models[0].Materials[1].UserData;
                            resFile.Models[loop].Materials[loopNumber].VolatileFlags = matres.Models[0].Materials[1].VolatileFlags;
                            resFile.Models[loop].Materials[loopNumber].TextureRefs = DeepClone(matres.Models[0].Materials[1].TextureRefs);
                            if (comboBox1.SelectedIndex == 0)
                            {
                                Syroot.NintenTools.Bfres.GX2.PolygonControl polygon = resFile.Models[loop].Materials[loopNumber].RenderState.PolygonControl;
                                polygon.CullBack = false;
                                polygon.CullFront = false;
                                resFile.Models[loop].Materials[loopNumber].RenderState.PolygonControl = polygon;
                            }
                            if (comboBox1.SelectedIndex == 1)
                            {
                                Syroot.NintenTools.Bfres.GX2.PolygonControl polygon = resFile.Models[loop].Materials[loopNumber].RenderState.PolygonControl;
                                polygon.CullBack = true;
                                polygon.CullFront = false;
                                resFile.Models[loop].Materials[loopNumber].RenderState.PolygonControl = polygon;
                            }
                            if (comboBox1.SelectedIndex == 2)
                            {
                                Syroot.NintenTools.Bfres.GX2.PolygonControl polygon = resFile.Models[loop].Materials[loopNumber].RenderState.PolygonControl;
                                polygon.CullBack = false;
                                polygon.CullFront = true;
                                resFile.Models[loop].Materials[loopNumber].RenderState.PolygonControl = polygon;
                            }
                            string str2 = "_alb";
                            Diffuse.TryGetValue(MaterialList[resFile.Models[loop].Materials[loopNumber].Name], out str2);
                            if (str2 == null)
                            {
                                str2 = "_alb";
                            }
                            resFile.Models[loop].Materials[loopNumber].TextureRefs[0].Name = str2;
                            string str3 = "_nrm";
                            Normal.TryGetValue(MaterialList[resFile.Models[loop].Materials[loopNumber].Name], out str3);
                            if (str3 == null)
                            {
                                if (str2 != "_alb")
                                {
                                    if (str2.Contains("_alb")) str3 = str2.Replace("_alb", "_nrm");
                                    else str3 = str2 + "_nrm";
                                }
                            }
                            resFile.Models[loop].Materials[loopNumber].TextureRefs[1].Name = str3;
                            string str4 = "_spc";
                            Specular.TryGetValue(MaterialList[resFile.Models[loop].Materials[loopNumber].Name], out str4);
                            if (str4 == null)
                            {
                                if (str2 != "_alb")
                                {
                                    if (str2.Contains("_alb")) str4 = str2.Replace("_alb", "_spc");
                                    else str4 = str2 + "_spc";
                                }
                            }
                            resFile.Models[loop].Materials[loopNumber].TextureRefs[2].Name = str4;
                            loopNumber++;
                        }
                        loop++;
                    }
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "Bfres File | *.bfres";
                    sfd.Title = "Where do you want to export the bfres?";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        resFile.Save(sfd.FileName);
                        MessageBox.Show("Saved to " + sfd.FileName + "!");
                        File.Delete("Material.Bfres");
                    }
                }
            }
        }
        public static T DeepClone<T>(T obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            return (T)JsonConvert.DeserializeObject<T>(json);
        }
        public bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
                return false;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }
    }
}
