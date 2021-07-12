﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using System.Linq;
using System;
using SpiritMod.Prim;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace SpiritMod.Items.Sets.FlailsMisc.JetBrick
{
    class JetBrickPrimTrail : PrimTrail
    {
        public JetBrickPrimTrail(Projectile projectile)
        {
			Entity = projectile;
			EntityType = projectile.type;
			DrawType = PrimTrailManager.DrawProjectile;
        }
        public override void SetDefaults()
        {
			Width = 12;
			Color = Color.White;
            AlphaValue= 1f;
			Cap = 12;
        }
        public override void PrimStructure(SpriteBatch spriteBatch)
        {
            /*if (PointCount <= 1) return; //for easier, but less customizable, drawing
            float colorSin = (float)Math.Sin(Counter / 3f);
            Color c1 = Color.Lerp(Color.White, Color.Cyan, colorSin);
            float widthVar = (float)Math.Sqrt(Points.Count) * Width;
            DrawBasicTrail(c1, widthVar);*/

            if (PointCount <= 6) return;
            float widthVar;
            for (int i = 0; i < Points.Count; i++)
            {
                if (i != 0)
                {
                    if (i != Points.Count - 1)
                    {
						widthVar = ((Width * (1 - ((float)(Points.Count - i) / (float)Points.Count))) + Width) * 0.5f;
                        Vector2 normal = CurveNormal(Points, i);
                        Vector2 normalAhead = CurveNormal(Points, i + 1);
                        Vector2 firstUp = Points[i] - normal * widthVar;
                        Vector2 firstDown = Points[i] + normal * widthVar;
                        Vector2 secondUp = Points[i + 1] - normalAhead * widthVar;
                        Vector2 secondDown = Points[i + 1] + normalAhead * widthVar;

						AddVertex(firstDown, Color * AlphaValue, new Vector2(i / (float)Points.Count, 1));
						AddVertex(firstUp, Color * AlphaValue, new Vector2(i / (float)Points.Count, 0));
						AddVertex(secondDown, Color * AlphaValue, new Vector2((i + 1) / (float)Points.Count, 1));

						AddVertex(secondUp, Color * AlphaValue, new Vector2((i + 1) / (float)Points.Count, 0));
						AddVertex(secondDown, Color * AlphaValue, new Vector2((i + 1) / (float)Points.Count, 1));
						AddVertex(firstUp, Color * AlphaValue, new Vector2(i / (float)Points.Count, 0));
					}
                }
            }
        }
       public override void SetShaders()
        {
			Effect effect = SpiritMod.JetbrickTrailShader;
			Color color;
			if (Counter < 60)
				color = Color.Lerp(Color.Orange, Color.Cyan, Counter / 60f);
			else
				color = Color.Lerp(Color.Cyan, Color.Red, Math.Min(Counter / 120f, 1));

			if (effect.HasParameter("noise"))
				effect.Parameters["noise"].SetValue(GetInstance<SpiritMod>().GetTexture("Textures/vnoise"));

			if (effect.HasParameter("circle"))
				effect.Parameters["circle"].SetValue(GetInstance<SpiritMod>().GetTexture("Effects/Masks/Extra_49"));
			PrepareShader(effect, "MainPS", Counter / 30f, color);
		}
        public override void OnUpdate()
        {
			if (Entity is Projectile proj && proj.active)
				Counter = (int)proj.localAI[0];
            PointCount= Points.Count() * 6;
            if (Cap < PointCount / 6)
            {
                Points.RemoveAt(0);
            }
            if ((!Entity.active && Entity != null) || Destroyed)
            {
                OnDestroy();
            }
            else
            {
                Points.Add(Entity.Center + Entity.velocity);
            }
        }
        public override void OnDestroy()
        {
			Destroyed = true;
			if (Points.Count < 2)
				Dispose();
			else
				Points.RemoveAt(0);
		}
    }
}