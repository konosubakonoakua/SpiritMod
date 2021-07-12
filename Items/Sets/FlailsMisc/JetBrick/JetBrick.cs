﻿using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using SpiritMod.Prim;

namespace SpiritMod.Items.Sets.FlailsMisc.JetBrick
{
	public class JetBrick : BaseFlailItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Jet Brick");
			Tooltip.SetDefault("Launches you forward");
		}

		public override void SafeSetDefaults()
		{
			item.Size = new Vector2(34, 30);
			item.damage = 60;
			item.rare = ItemRarityID.Green;
			item.useTime = 30;
			item.useAnimation = 30;
			item.shoot = ModContent.ProjectileType<JetBrickProj>();
			item.shootSpeed = 16;
			item.knockBack = 4;
		}
	}
	public class JetBrickProj : BaseFlailProj
	{
		public JetBrickProj() : base(new Vector2(0.7f, 1.6f), new Vector2(0.5f, 3f), 2, 70, 13) { }

		public override void SetStaticDefaults() => DisplayName.SetDefault("Jet Brick");

		public override void SpinExtras(Player player)
		{
			if (projectile.localAI[0]++ == 0)
			{
				SpiritMod.primitives.CreateTrail(new JetBrickPrimTrail(projectile));
			}
			AddColor();
		}

		public override void NotSpinningExtras(Player player)
		{
			AddColor();
		}

		public override void LaunchExtras(Player player)
		{
			if (projectile.Distance(player.Center) > 200 && projectile.localAI[0] > 100)
				player.velocity = projectile.velocity * 0.7f;
			base.LaunchExtras(player);
		}

		private void AddColor()
		{
			Color color;
			if (projectile.localAI[0] < 60)
				color = Color.Lerp(Color.Orange, Color.Cyan, projectile.localAI[0] / 60f);
			else
				color = Color.Lerp(Color.Cyan, Color.Red, Math.Min(projectile.localAI[0] / 120f, 1));
			Lighting.AddLight(projectile.Center, color.ToVector3());
		}
	}
}
