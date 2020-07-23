﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiritMod.Buffs;
using Terraria;
using Terraria.ModLoader;

namespace SpiritMod.Projectiles.Flail
{
	public class ClatterMaceProj : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Clattering Mace");
		}

		public override void SetDefaults()
		{
			projectile.width = 30;
			projectile.height = 18;
			projectile.friendly = true;
			projectile.penetrate = -1;
			projectile.melee = true;
		}

		public override bool PreAI()
		{
			ProjectileExtras.FlailAI(projectile.whoAmI);
			return false;
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			return ProjectileExtras.FlailTileCollide(projectile.whoAmI, oldVelocity);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			ProjectileExtras.DrawChain(projectile.whoAmI, Main.player[projectile.owner].MountedCenter,
				"SpiritMod/Projectiles/Flail/ClatterMace_Chain");
			ProjectileExtras.DrawAroundOrigin(projectile.whoAmI, lightColor);
			return false;
		}
	}
}
