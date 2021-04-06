﻿using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace SpiritMod.Particles
{
	public class FireParticle : Particle
	{
		private static readonly int MaxTime = 540;

		private Vector2 centerPlusPosition;
		private Vector2 spawnPosition;

		public override void Update()
		{
			if (TimeActive >= MaxTime)
				Kill();

			Vector2 worldPosition = centerPlusPosition - Main.screenPosition;
			Vector2 screenPosition = spawnPosition;
			Position = Vector2.Lerp(screenPosition, screenPosition - 3 * (screenPosition - worldPosition), Scale);

			centerPlusPosition += Velocity;
			spawnPosition += Velocity;

			Color = Color.White * (float)Math.Sin(MathHelper.TwoPi * ((MaxTime - TimeActive) / (float)MaxTime)) * 0.85f;
		}

		public override float SpawnChance() => Main.LocalPlayer.ZoneMeteor || Main.LocalPlayer.ZoneUnderworldHeight ? 0.05f : 0;

		public override void OnSpawnAttempt()
		{
			FireParticle fireParticle = new FireParticle();

			Vector2 startingPosition = new Vector2(Main.rand.Next(-2000, 2000), Main.rand.Next(1200, 1800));
			fireParticle.Position = startingPosition;
			fireParticle.Velocity = new Vector2(Main.windSpeed * 2f, Main.rand.NextFloat(-2, -6f));
			fireParticle.Rotation = Main.rand.NextFloat(MathHelper.PiOver4);
			fireParticle.Scale = Main.rand.NextFloat(0.3f, 0.85f);
			fireParticle.Color = Color.White;

			fireParticle.centerPlusPosition = Main.LocalPlayer.Center + startingPosition;
			fireParticle.spawnPosition = startingPosition;

			ParticleHandler.SpawnParticle(fireParticle);
		}
	}
}