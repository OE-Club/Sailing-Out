using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SailingOut.Editor
{
    /// <summary>Original, deliberately pixel-authored release art. No external/browser artwork is used.</summary>
    public static class OriginalPixelArt
    {
        private const string ArtRoot = "Assets/SailingOut/Art";
        private static readonly Color32 Ink = Hex("122F43");
        private static readonly Color32 Dark = Hex("24536A");
        private static readonly Color32 Rock = Hex("427789");
        private static readonly Color32 LightRock = Hex("70A2A6");
        private static readonly Color32 Foam = Hex("9AD6CB");
        private static readonly Color32 White = Hex("E8F0D8");
        private static readonly Color32 Shade = Hex("ABC9BE");
        private static readonly Color32 Red = Hex("DF785F");
        private static readonly Color32 RedDark = Hex("A74F4B");
        private static readonly Color32 Yellow = Hex("E8C77D");
        private static readonly Color32 Wood = Hex("AE8059");
        private static readonly Color32 Teal = Hex("409F95");
        private static readonly Color32 ChargeGreen = Hex("71D96C");
        private static readonly Color32 ChargeHighlight = Hex("D6F58A");

        [MenuItem("Sailing Out/Create Missing Original Pixel Art")]
        public static void Generate()
        {
            var all = new List<Canvas>();
            for (int variant = 1; variant <= 3; variant++)
            {
                Frames("buoy", variant, 2, Buoy, all);
                Frames("reef", variant, 1, Reef, all);
                Frames("seabird", variant, 4, Bird, all);
                Frames("supply", variant, 2, Supply, all);
                Frames("lightning", variant, 2, Lightning, all);
                Frames("decoration", variant, 1, Debris, all);
            }
            Write(Water(false), ArtRoot + "/Textures/water_tile.png");
            Write(Water(true), ArtRoot + "/Textures/foam_tile.png");
            Write(Boat(), ArtRoot + "/Demo/boat_demo.png");
            AssetDatabase.Refresh();
            SailingOutAssetBuilder.BuildDefault();
        }

        private static void Frames(string name, int variant, int count, Func<int, int, Canvas> draw, List<Canvas> all)
        {
            var sheet = new Canvas(32 * count, 32);
            for (int frame = 0; frame < count; frame++)
            {
                Canvas art = draw(variant, frame);
                Write(art, $"{ArtRoot}/Sprites/{name}_{variant:00}_{frame + 1:00}.png");
                sheet.Copy(art, frame * 32, 0);
                all.Add(art);
            }
            Write(sheet, $"{ArtRoot}/SpriteSheets/{name}_{variant:00}_sheet.png");
        }

        private static Canvas Buoy(int variant, int frame)
        {
            var c = new Canvas(32, 32);
            int bob = frame;
            c.Line(8, 25, 12, 26, Dark); c.Line(20, 26, 24, 25, Dark);
            c.Line(7, 27, 13, 28, Foam); c.Line(19, 28, 25, 27, Foam);
            c.Oval(10, 20 + bob, 12, 5, Ink);
            c.Oval(11, 20 + bob, 10, 3, Teal);
            Color32 primary = variant == 1 ? Red : variant == 2 ? Yellow : Teal;
            c.Poly(Ink, 11,21+bob, 10,17+bob, 13,11+bob, 19,11+bob, 22,17+bob, 21,21+bob);
            c.Poly(primary, 12,20+bob, 11,17+bob, 14,12+bob, 18,12+bob, 21,17+bob, 20,20+bob);
            c.Rect(12, 17+bob, 8, 2, White);
            c.Line(13,14+bob, 14,13+bob, White);
            c.Line(20,19+bob, 19,20+bob, variant == 1 ? RedDark : Dark);
            c.Rect(15, 6+bob, 2, 7, Ink);
            c.Rect(16, 6+bob, 1, 6, Shade);
            if (variant == 1)
                c.Poly(Red, 17,6+bob, 23,8+bob, 17,10+bob);
            else if (variant == 2)
            {
                c.Rect(13, 6+bob, 6, 4, Ink); c.Rect(14, 7+bob, 4, 2, Yellow);
                c.Set(15, 6+bob, White);
            }
            else
            {
                c.Poly(Ink, 16,2+bob, 20,6+bob, 16,10+bob, 12,6+bob);
                c.Poly(Teal, 16,3+bob, 19,6+bob, 16,9+bob, 13,6+bob);
                c.Set(16, 5+bob, White);
            }
            return c;
        }

        private static Canvas Reef(int variant, int frame)
        {
            var c = new Canvas(32, 32);
            c.Oval(3, 7, 26, 22, Dark);
            c.Line(4,21, 6,25, Foam); c.Line(7,26, 12,27, Foam);
            c.Line(23,24, 27,20, Foam); c.Line(27,14, 27,17, Teal);
            if (variant == 1)
            {
                c.Poly(Ink, 5,19, 7,10, 13,5, 21,7, 26,16, 23,23, 13,25);
                c.Poly(Rock, 7,18, 9,11, 14,7, 20,9, 23,16, 21,21, 13,23);
                c.Poly(LightRock, 9,12, 14,8, 19,10, 16,13, 11,14);
                c.Poly(Dark, 17,15, 22,15, 21,21, 13,22);
                c.Line(13,15, 11,19, Ink); c.Line(12,19, 15,20, Ink);
            }
            else if (variant == 2)
            {
                c.Poly(Ink, 5,19, 8,12, 12,12, 15,4, 21,3, 27,11, 25,23, 18,26, 8,24);
                c.Poly(Rock, 7,20, 10,14, 14,14, 17,6, 21,5, 25,12, 23,22, 17,24, 10,22);
                c.Poly(LightRock, 17,7, 20,6, 23,10, 18,11, 15,17, 14,14);
                c.Poly(Dark, 21,14, 24,13, 22,22, 18,23, 18,19);
                c.Line(10,18, 13,17, LightRock); c.Line(18,14, 17,19, Ink);
            }
            else
            {
                c.Poly(Ink, 3,16, 7,7, 15,5, 20,10, 26,12, 29,21, 24,26, 17,24, 11,27, 5,23);
                c.Poly(Rock, 5,16, 8,9, 14,7, 19,12, 24,14, 27,21, 23,24, 17,22, 11,25, 7,21);
                c.Poly(LightRock, 8,10, 13,8, 17,12, 11,13, 7,17);
                c.Poly(LightRock, 20,15, 23,15, 25,19, 21,19);
                c.Line(15,14, 13,20, Ink); c.Line(14,20, 18,21, Ink);
            }
            c.Rect(9,21, 3,1, Teal); c.Set(10,20,Teal);
            c.Rect(20,10, 2,1, White);
            return c;
        }

        private static Canvas Bird(int variant, int frame)
        {
            var c = new Canvas(32, 32);
            int tip = new[] { 5, 12, 21, 12 }[frame];
            int span = variant == 2 ? 13 : variant == 3 ? 10 : 12;
            Color32 wing = variant == 3 ? Shade : White;
            c.Poly(Ink, 15,13, 11,11, 16-span,tip, 15-span,tip+3, 8,18, 14,19);
            c.Poly(wing, 14,14, 11,13, 17-span,tip+2, 9,17, 14,17);
            c.Poly(Ink, 17,13, 21,11, 16+span,tip, 17+span,tip+3, 24,18, 18,19);
            c.Poly(wing, 18,14, 21,13, 15+span,tip+2, 23,17, 18,17);
            c.Poly(Ink, 15,10, 18,10, 19,15, 18,21, 20,24, 15,23, 13,24, 15,20, 14,15);
            c.Poly(White, 16,11, 17,11, 18,15, 17,21, 15,22, 16,19, 15,15);
            c.Line(17,16, 17,20, Shade);
            c.Rect(16,8, 2,3, Yellow); c.Set(17,11,Ink);
            c.Set(16-span,tip+1, Ink); c.Set(16+span,tip+1, Ink);
            return c;
        }

        private static Canvas Supply(int variant, int frame)
        {
            var c = new Canvas(32, 32);
            c.Line(7,24, 12,25, Dark); c.Line(20,25, 25,24, Foam);
            if (variant == 1)
            {
                c.Rect(8,10,16,14,Ink); c.Rect(9,11,14,12,Wood);
                c.Rect(10,12,12,2,Yellow); c.Rect(10,21,12,1,Yellow);
                c.Rect(11,14,1,7,Ink); c.Rect(20,14,1,7,Ink);
                c.Line(12,14, 19,20, Yellow); c.Line(19,14, 12,20, Yellow);
                c.Rect(14,9,4,3,Ink); c.Rect(15,10,2,1,Shade);
            }
            else if (variant == 2)
            {
                c.Oval(7,9,18,17,Ink); c.Oval(8,10,16,15,Red);
                c.Rect(9,14,14,5,White); c.Rect(13,11,5,13,White);
                c.Oval(12,13,8,9,Ink); c.Oval(13,14,6,7,Teal);
                c.Line(10,12, 11,11,White); c.Line(20,23, 22,21,RedDark);
            }
            else
            {
                c.Poly(Ink, 10,11, 22,11, 25,22, 21,25, 10,24, 7,21);
                c.Poly(Shade, 11,12, 21,12, 23,21, 20,23, 11,22, 9,20);
                c.Rect(12,9,8,3,Ink); c.Rect(13,10,6,2,Wood);
                c.Rect(14,13,4,10,Wood); c.Rect(11,16,10,3,Yellow);
                c.Rect(15,16,2,3,White);
            }
            Color32 glint = frame == 0 ? Yellow : White;
            int x = frame == 0 ? 25 : 5;
            c.Line(x,5, x,9,glint); c.Line(x-2,7, x+2,7,glint);
            c.Set(frame == 0 ? 5 : 26, 18, Yellow);
            return c;
        }

        private static Canvas Lightning(int variant, int frame)
        {
            var c = new Canvas(32, 32);
            int x = variant - 2;
            Color32 glow = frame == 0 ? ChargeGreen : ChargeHighlight;
            if (variant == 1)
            {
                c.Oval(5, 5, 22, 22, Dark);
                c.Oval(7, 7, 18, 18, Teal);
            }
            else if (variant == 2)
            {
                c.Line(4, 15, 8, 15, glow);
                c.Line(24, 15, 28, 15, glow);
                c.Line(16, 1, 16, 5, glow);
                c.Line(16, 26, 16, 30, glow);
            }
            else
            {
                c.Poly(Dark, 16, 2, 29, 16, 16, 29, 3, 16);
                c.Poly(Teal, 16, 5, 26, 16, 16, 26, 6, 16);
            }

            c.Poly(Ink, 14+x, 4, 23+x, 4, 18+x, 13, 25+x, 13,
                11+x, 29, 14+x, 18, 8+x, 18);
            c.Poly(ChargeGreen, 15+x, 6, 21+x, 6, 16+x, 15, 22+x, 15,
                13+x, 25, 16+x, 16, 11+x, 16);
            c.Line(16+x, 7, 18+x, 7, ChargeHighlight);
            c.Line(15+x, 18, 14+x, 21, ChargeHighlight);
            c.Set(frame == 0 ? 6 : 25, 8, ChargeHighlight);
            c.Set(frame == 0 ? 25 : 6, 23, glow);
            return c;
        }

        private static Canvas Debris(int variant, int frame)
        {
            var c = new Canvas(32, 32);
            if (variant == 1)
            {
                c.Line(6,20, 22,12, Dark); c.Line(7,21, 24,13, Wood);
                c.Line(8,22, 24,14, Wood); c.Line(9,21, 23,14, Yellow);
                c.Line(17,18, 21,21, Dark); c.Line(18,18, 22,21, Wood);
            }
            else if (variant == 2)
            {
                c.Line(10,24, 20,9, Dark); c.Line(15,19, 9,14,Teal);
                c.Line(17,16, 24,14,Teal); c.Line(13,22, 7,19,Teal);
                c.Line(19,12, 17,8,Teal); c.Line(17,16, 23,19,Teal);
                c.Set(12,18, Foam); c.Set(21,15, Foam);
            }
            else
            {
                c.Poly(Dark, 6,12, 13,10, 12,15, 9,16);
                c.Line(8,12, 11,12,Teal);
                c.Poly(Teal, 18,18, 24,16, 23,21, 19,22);
                c.Set(20,18,Foam); c.Set(14,24,Teal); c.Set(13,24,Teal);
            }
            return c;
        }

        private static Canvas Water(bool overlay)
        {
            var c = new Canvas(128, 128);
            var random = new SeededRandom(overlay ? 71 : 46);
            if (!overlay)
                c.Rect(0,0,128,128,Hex("237887"));
            for (int i = 0; i < (overlay ? 7 : 70); i++)
            {
                int x = random.Range(0,128), y = random.Range(0,128), length = random.Range(3,10);
                Color32 color = overlay ? (i % 3 == 0 ? Foam : Hex("57A79F")) :
                    i % 3 == 0 ? Hex("2C8490") : i % 3 == 1 ? Hex("1F7082") : Hex("328D96");
                for (int k = 0; k < length; k++)
                    c.SetWrap(x+k, y + (k == 0 || k == length-1 ? 1 : 0),color);
                if (overlay)
                    c.SetWrap(x+length+2,y+2,color);
                else if (i % 4 == 0)
                    for (int k = 2; k < length-2; k++) c.SetWrap(x+k,y+3,Hex("2C8490"));
            }
            return c;
        }

        private static Canvas Boat()
        {
            var c = new Canvas(32,48);
            c.Line(8,35, 5,42,Foam); c.Line(24,35,27,42,Foam);
            c.Line(12,42, 10,46,Shade); c.Line(20,43,22,46,Shade);
            c.Poly(Ink, 16,3, 24,23, 23,36, 19,41, 12,40, 8,34, 8,24);
            c.Poly(Wood, 16,6, 22,24, 21,35, 18,38, 13,37, 10,33, 10,24);
            c.Poly(Yellow, 16,7, 19,18, 12,22);
            c.Line(12,27, 12,34,Yellow); c.Line(19,28,19,35,Dark);
            c.Rect(14,30,4,7,Ink); c.Rect(15,31,2,4,Red);
            c.Line(16,9, 16,31,Ink);
            c.Poly(Ink, 15,9, 3,28, 16,28);
            c.Poly(White, 14,13, 5,26, 14,25);
            c.Line(7,25, 13,23,Shade);
            c.Poly(Ink, 18,12, 29,27, 18,26);
            c.Poly(Red, 19,15, 27,25, 19,24);
            c.Line(20,19, 23,23,Yellow);
            c.Line(16,6, 16,33,Ink); c.Line(17,7,17,32,Yellow);
            c.Poly(Red, 17,5, 24,7, 17,9);
            return c;
        }

        private static Color32 Hex(string value) => new Color32(Convert.ToByte(value.Substring(0,2),16),
            Convert.ToByte(value.Substring(2,2),16), Convert.ToByte(value.Substring(4,2),16),255);

        private static void Write(Canvas canvas, string path)
        {
            // Rebuilds deliberately preserve an artist's edited PNGs.
            if (File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false);
            texture.SetPixels32(canvas.Pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private sealed class Canvas
        {
            public readonly int Width, Height;
            public readonly Color32[] Pixels;
            public Canvas(int width, int height) { Width=width; Height=height; Pixels=new Color32[width*height]; }
            // Drawing coordinates originate at top-left; PNG/Unity arrays originate at bottom-left.
            public void Set(int x, int y, Color32 color)
            {
                if (x>=0 && y>=0 && x<Width && y<Height) Pixels[(Height-1-y)*Width+x]=color;
            }
            public void SetWrap(int x, int y, Color32 color) => Set((x%Width+Width)%Width,(y%Height+Height)%Height,color);
            public void Rect(int x,int y,int w,int h,Color32 color)
            { for(int j=y;j<y+h;j++) for(int i=x;i<x+w;i++) Set(i,j,color); }
            public void Line(int x,int y,int endX,int endY,Color32 color)
            {
                int dx=Math.Abs(endX-x), sx=x<endX?1:-1, dy=-Math.Abs(endY-y), sy=y<endY?1:-1, error=dx+dy;
                while(true) { Set(x,y,color); if(x==endX && y==endY) break;
                    int twice=2*error; if(twice>=dy){error+=dy;x+=sx;} if(twice<=dx){error+=dx;y+=sy;} }
            }
            public void Oval(int x,int y,int w,int h,Color32 color)
            { for(int j=0;j<h;j++) for(int i=0;i<w;i++)
                if(Mathf.Pow((i+0.5f-w*0.5f)/(w*0.5f),2)+Mathf.Pow((j+0.5f-h*0.5f)/(h*0.5f),2)<=1) Set(x+i,y+j,color); }
            public void Poly(Color32 color,params int[] xy)
            {
                for(int y=0;y<Height;y++) for(int x=0;x<Width;x++)
                {
                    bool inside=false; int j=xy.Length-2;
                    for(int i=0;i<xy.Length;i+=2) { if((xy[i+1]>y+0.5f)!=(xy[j+1]>y+0.5f) &&
                        x+0.5f<(float)(xy[j]-xy[i])*(y+0.5f-xy[i+1])/(xy[j+1]-xy[i+1])+xy[i]) inside=!inside; j=i; }
                    if(inside) Set(x,y,color);
                }
            }
            public void Copy(Canvas other,int x,int y)
            { for(int j=0;j<other.Height;j++) for(int i=0;i<other.Width;i++) Set(x+i,y+j,other.Pixels[(other.Height-1-j)*other.Width+i]); }
        }
    }
}
