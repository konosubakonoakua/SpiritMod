﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiritMod.Mechanics.BackgroundSystem;
using SpiritMod.NPCs.StarjinxEvent.Enemies.Archon.Projectiles;
using SpiritMod.NPCs.StarjinxEvent.Enemies.Starachnid;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using SpiritMod.Particles;
using Terraria.Utilities;

namespace SpiritMod.NPCs.StarjinxEvent.Enemies.Archon
{
	public class Archon : SpiritNPC, IDrawAdditive
	{
		// CONSTS ----------

		// FG/BG Related
		public const int MaxForegroundTransitionTime = 1200;

		// Stage IDs
		public const int InitStage = 0;
		public const int EnchantAttackStage = 1;
		public const int VulnerableStage = 2;

		// Stage Lengths
		public const int EnchantMaxTime = 1200;

		// Fields
		internal int stage = InitStage;

		private ref Player Target => ref Main.player[npc.target];

		// FG/BG Related
		internal bool inFG = true;
		private bool transitionFG = false;
		private int fgTime = 0;
		private Vector2 fgPos = new Vector2();
		private ArchonBG Background = null;

		// Enchantment
		internal Enchantment enchantment = Enchantment.None;

		// ATTACKS --------------------
		internal bool waitingOnAttack = false;
		private AttackType attack = AttackType.None;
		private int realDamage = 0;
		private float attackTimeMax = 0;

		// Basic Slash
		private Vector2 cachedAttackPos = new Vector2();

		// Starlight Constellation
		private List<StarThread> threads = new List<StarThread>();
		private int currentThread = 0;
		private float currentThreadProgress = 0f;

		// Meteor Dash
		private Vector2 meteorDashOffset = Vector2.Zero;
		private Vector2 meteorDashBegin = Vector2.Zero;
		private float meteorDashFactor = 0f;

		// Misc
		private readonly Dictionary<string, int> timers = new Dictionary<string, int>() { { "ENCHANT", 0 }, { "ATTACK", 0 }, { "CONSTELLATION", 0 } };

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Archon");
			Main.npcFrameCount[npc.type] = 6;
		}

		public override void SetDefaults()
		{
			npc.width = 190;
			npc.height = 114;
			npc.damage = 20;
			npc.defense = 28;
			npc.lifeMax = 12000;
			npc.aiStyle = -1;
			npc.HitSound = SoundID.DD2_CrystalCartImpact;
			npc.DeathSound = SoundID.DD2_ExplosiveTrapExplode;
			npc.value = Item.buyPrice(0, 0, 15, 0);
			npc.knockBackResist = 0f;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.boss = true;
		}

		public override bool CheckActive() => !ModContent.GetInstance<StarjinxEventWorld>().StarjinxActive;

		public override void AI()
		{
			if (Background == null) //init stuff
			{
				Background = new ArchonBG(npc.Center, npc);
				BackgroundItemManager.AddItem(Background);
			}
			//ArchonLiving = NPC.AnyNPCs(ModContent.NPCType<Archon>());

			npc.TargetClosest(true);
			npc.dontTakeDamage = !inFG;

			if (realDamage > 0)
				npc.damage = realDamage;

			if (transitionFG)
			{
				Transition();
				return;
			}

			if (inFG)
				UpdateForeground();
			else
				UpdateBackground();
		}

		#region Background Stuff
		private void Transition()
		{
			npc.velocity.Y -= 0.06f;

			if (npc.Center.Y < -200 && fgTime > MaxForegroundTransitionTime / 2)
				fgTime = MaxForegroundTransitionTime / 2;

			if (fgTime == MaxForegroundTransitionTime / 2)
			{
				inFG = !inFG;
				npc.position.Y = -150;
			}

			if (fgTime < MaxForegroundTransitionTime / 2)
			{
				npc.Center = Vector2.Lerp(npc.Center, fgPos, 0.125f);
				npc.velocity *= 0f;
			}

			if (fgTime == 0)
				transitionFG = false;
		}

		private void ToggleForeground()
		{
			transitionFG = true;
			fgTime = MaxForegroundTransitionTime;
			fgPos = npc.Center;
		}

		private void UpdateBackground()
		{
		}
		#endregion

		private void UpdateForeground()
		{
			switch (stage)
			{
				case EnchantAttackStage:
					EnchantBehaviour();
					break;
				default:
					break;
			}
		}

		private void EnchantBehaviour()
		{
			timers["ENCHANT"]++;

			if (attack == AttackType.None)
				SetupRandomAttack();
			else if (enchantment != Enchantment.None)
				timers["ATTACK"]++;

			npc.velocity.Y = (float)Math.Sin(timers["ENCHANT"] * 0.1f) * 0.2f;

			switch (enchantment)
			{
				case Enchantment.Starlight:
					StarlightBehaviour();
					break;
				case Enchantment.Meteor:
					MeteorBehaviour();
					break;
				default:
					break;
			}

			if (timers["ENCHANT"] == EnchantMaxTime && !waitingOnAttack)
			{
				stage = VulnerableStage;
				timers["ENCHANT"] = 0;
			}
		}

		private void MeteorBehaviour()
		{
			switch (attack)
			{
				case AttackType.TeleportSlash:
					SlashAttack();
					break;
				case AttackType.Cast:
					CastAttack();
					break;
				case AttackType.MeteorDash:
					MeteorDashAttack();
					break;
				default:
					break;
			}
		}

		private void MeteorDashAttack()
		{
			const float AnticipationThreshold = 0.15f;
			const float MeteorRainThreshold = 0.95f;

			if (timers["ATTACK"] < (int)(attackTimeMax * AnticipationThreshold)) //Anticipation & move to position
			{
				if (meteorDashOffset == Vector2.Zero)
					meteorDashOffset = new Vector2(0, -Main.rand.Next(300, 400)).RotatedByRandom(MathHelper.PiOver2);

				npc.Center = Vector2.Lerp(npc.Center, Target.Center + meteorDashOffset, 0.15f);
			}
			else if (timers["ATTACK"] == (int)(attackTimeMax * AnticipationThreshold)) //Dash initialization
			{
				waitingOnAttack = true;
				cachedAttackPos = npc.Center - ((npc.Center - Target.Center) * 2.5f);
				meteorDashBegin = npc.Center;
			}
			else if (timers["ATTACK"] >= attackTimeMax * AnticipationThreshold && timers["ATTACK"] < attackTimeMax * MeteorRainThreshold - 2) //Actual dash
			{
				meteorDashFactor = MathHelper.Lerp(meteorDashFactor, 1f, 0.15f);
				npc.velocity = npc.DirectionTo(cachedAttackPos) * 30 * meteorDashFactor;

				if (npc.DistanceSQ(cachedAttackPos) > 32 * 32)
					realDamage = 70;
				else
				{
					realDamage = 0;
					timers["ATTACK"] = (int)(attackTimeMax * MeteorRainThreshold) - 2;
				}
			}
			else if (timers["ATTACK"] >= (int)(attackTimeMax * MeteorRainThreshold) && timers["ATTACK"] < attackTimeMax)
			{
				if (timers["ATTACK"] == (int)(attackTimeMax * MeteorRainThreshold)) //Spawn meteors
				{
					for (int i = 0; i < 4; ++i)
					{
						Vector2 vel = npc.DirectionFrom(meteorDashBegin).RotatedByRandom(0.1f) * Main.rand.NextFloat(6f, 17f);
						Vector2 pos = meteorDashBegin - (vel * 30) - new Vector2(Main.rand.Next(-300, 300), 0);
						Projectile.NewProjectile(pos, vel, ModContent.ProjectileType<MeteorEnchantment_Meteor>(), 20, 1f);
					}
				}

				npc.velocity *= 0.8f; //Slow down
			}
			else if (timers["ATTACK"] >= attackTimeMax)
			{
				timers["ATTACK"] = 0;
				attack = AttackType.None;
				waitingOnAttack = false;

				meteorDashOffset = Vector2.Zero;
				meteorDashBegin = Vector2.Zero;
				meteorDashFactor = 0;
			}
		}

		#region Starlight Attacks
		private void StarlightBehaviour()
		{
			switch (attack)
			{
				case AttackType.TeleportSlash:
					SlashAttack();
					break;
				case AttackType.Cast:
					CastAttack();
					break;
				case AttackType.StarlightConstellation:
					StarlightConstellationAttack();
					break;
				case AttackType.StarlightShootingStar:
					StarlightShootingStarAttack();
					break;
				default:
					break;
			}
		}

		private void StarlightShootingStarAttack()
		{
			const float CastWaitThreshold = 0.4f;

			if (timers["ATTACK"] == (int)(attackTimeMax * CastWaitThreshold)) //Shoot bg star
			{
				Projectile.NewProjectile(Target.Center, Vector2.Zero, ModContent.ProjectileType<BGStarProjectile>(), 20, 1f);
			}
			else if (timers["ATTACK"] >= attackTimeMax)
			{
				timers["ATTACK"] = 0;
				attack = AttackType.None;
				waitingOnAttack = false;
			}
		}

		private void StarlightConstellationAttack()
		{
			const float MaxSetupThreadsThreshold = 0.5f;
			const float SetupThreadsThreshold = 0.125f;

			waitingOnAttack = true;

			if (threads.Count > 0)
			{
				foreach (StarThread thread in threads)
					thread.Update();

				foreach (StarThread thread in threads.ToArray())
					if (thread.Counter > thread.Duration)
						threads.Remove(thread);
			}

			if (timers["ATTACK"] <= (int)(attackTimeMax * MaxSetupThreadsThreshold))
			{
				npc.velocity *= 0.98f;

				if (timers["ATTACK"] % (int)(attackTimeMax * SetupThreadsThreshold) == 0) //Setup threads
					AddThread();
			}
			else if (timers["ATTACK"] > attackTimeMax * MaxSetupThreadsThreshold && timers["ATTACK"] < attackTimeMax)
			{
				timers["CONSTELLATION"]++;
				realDamage = 100;

				float singleAttackThreshold = attackTimeMax * (1 - MaxSetupThreadsThreshold) / threads.Count;
				float threadProgress = timers["CONSTELLATION"] / singleAttackThreshold;

				npc.Center = Vector2.Lerp(npc.Center, threads[currentThread].EndPoint, currentThreadProgress);
				currentThreadProgress = threadProgress;

				float finalRot = (threads[currentThread].EndPoint - threads[currentThread].StartPoint).ToRotation();
				if (currentThread == threads.Count - 1)
					finalRot = 0;
				npc.rotation = MathHelper.Lerp(npc.rotation, finalRot, 0.15f);

				if (currentThreadProgress > 1f)
				{
					currentThread++;
					currentThreadProgress = 0;
					timers["CONSTELLATION"] = 0;

					if (currentThread >= threads.Count)
					{
						timers["ATTACK"] = (int)attackTimeMax + 2;
						currentThread = 0;
					}
				}
			}
			else if (timers["ATTACK"] >= attackTimeMax)
			{
				threads.Clear();
				currentThread = 0;
				timers["ATTACK"] = 0;
				attack = AttackType.None;
				waitingOnAttack = false;
				npc.rotation = 0f;

				realDamage = 0;
			}
		}

		private void AddThread()
		{
			Vector2 startPos = npc.Center;
			if (threads.Count > 0)
				startPos = threads[threads.Count - 1].EndPoint;

			Vector2 endPoint = GetThreadEndPoint(threads.Count == 0);
			threads.Add(new StarThread(startPos, endPoint));
		}

		private Vector2 GetThreadEndPoint(bool start)
		{
			if (start)
				return npc.Center - (npc.DirectionFrom(Target.Center) * (npc.Distance(Target.Center) + 420));
			else
			{
				int id = NPC.FindFirstNPC(ModContent.NPCType<StarjinxMeteorite>());
				Vector2 centre = Target.Center;

				if (id >= 0)
					centre = Target.GetModPlayer<StarjinxPlayer>().StarjinxPosition;

				float Size() => StarjinxMeteorite.EVENT_RADIUS * Main.rand.NextFloat(0.7f, 0.85f);

				Vector2 nextPos = centre + Main.rand.NextVector2CircularEdge(Size(), Size());

				while (Vector2.DistanceSquared(threads[threads.Count - 1].EndPoint, nextPos) < 300 * 300)
					nextPos = centre + Main.rand.NextVector2CircularEdge(Size(), Size());
				return nextPos;
			}
		}
		#endregion

		private void CastAttack()
		{
			float AnimationWaitThreshold = 0.8f;

			waitingOnAttack = true;
			npc.rotation = MathHelper.Lerp(npc.rotation, 0f, 0.2f);
			npc.velocity *= 0.95f;

			if (timers["ATTACK"] == (int)(attackTimeMax * AnimationWaitThreshold)) //Wait for animation to finish then shoot
			{
				if (enchantment == Enchantment.Starlight)
				{
					Vector2 baseVel = npc.DirectionTo(Target.Center) * 38;

					int reps = Main.rand.Next(10, 14);
					for (int i = 0; i < reps; ++i)
					{
						//Projectiles have lower velocity the further they deviate from the base velocity, forming a v-like shape
						const float maxAngularOffset = MathHelper.PiOver4;
						float angularOffset = Main.rand.NextFloat(-1, 1) * maxAngularOffset;
						Vector2 velocity = baseVel.RotatedBy(angularOffset) * (1 - Math.Abs(angularOffset / maxAngularOffset)) * Main.rand.NextFloat(0.75f, 1.1f);

						Projectile.NewProjectile(npc.Center, velocity, ModContent.ProjectileType<ArchonStarFragment>(), 20, 1f);

						if (!Main.dedServ)
						{
							int numParticles = 2;
							for (int j = 0; j < numParticles; j++)
								ParticleHandler.SpawnParticle(new StarParticle(npc.Center, velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.5f), Color.White, Color.Cyan, Main.rand.NextFloat(0.1f, 0.2f), 25));
						}
					}

					npc.velocity -= baseVel / 4; //Recoil effect
				}
			}
			else if (timers["ATTACK"] >= attackTimeMax)
			{
				timers["ATTACK"] = 0;
				attack = AttackType.None;
				waitingOnAttack = false;

				realDamage = 0;
			}
		}

		private void SlashAttack()
		{
			const float AnticipationThreshold = 0.1f;
			const float BeginAttackThreshold = 0.3f;

			if (timers["ATTACK"] == (int)(attackTimeMax * AnticipationThreshold)) //Teleport & start slash anticipation
			{
				waitingOnAttack = true;
				npc.Center = Target.Center + new Vector2(0, Main.rand.Next(300, 400)).RotatedByRandom(MathHelper.Pi);
				cachedAttackPos = npc.Center - (npc.DirectionFrom(Target.Center) * npc.Distance(Target.Center) * 2);
			}
			else if (timers["ATTACK"] > attackTimeMax * AnticipationThreshold && timers["ATTACK"] < attackTimeMax * BeginAttackThreshold)
			{
				float mult = (attackTimeMax * BeginAttackThreshold) - timers["ATTACK"];
				npc.velocity = -npc.DirectionTo(cachedAttackPos) * 0.5f * mult;
				npc.rotation = MathHelper.Lerp(npc.rotation, npc.AngleTo(cachedAttackPos), 0.2f);
			}
			else if (timers["ATTACK"] >= (int)(attackTimeMax * BeginAttackThreshold))
			{
				npc.Center = Vector2.Lerp(npc.Center, cachedAttackPos, 0.075f);

				if (npc.DistanceSQ(cachedAttackPos) > 10 * 10)
					realDamage = 70;
				else
				{
					realDamage = 0;
					timers["ATTACK"] = (int)attackTimeMax;
				}
			}

			if (timers["ATTACK"] >= attackTimeMax)
			{
				timers["ATTACK"] = 0;
				attack = AttackType.None;
				waitingOnAttack = false;

				npc.velocity = Vector2.Zero;
				realDamage = 0;
			}
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => realDamage > 0;

		public override void FindFrame(int frameHeight)
		{
			if (npc.frameCounter++ > 6)
			{
				npc.frameCounter = 0;
				npc.frame.Y += frameHeight;

				if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
					npc.frame.Y = 0;
			}
		}

		#region Drawing
		public void AdditiveCall(SpriteBatch sB)
		{
			if (!inFG) //if not in foreground stop drawing
				return;

			DrawThreads(sB);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			if (!inFG) //if not in foreground stop drawing
				return false;

			float realRot = npc.rotation;
			SpriteEffects effect = SpriteEffects.None;

			if (npc.rotation < -MathHelper.PiOver2 || npc.rotation > MathHelper.PiOver2)
			{
				realRot -= MathHelper.Pi;
				effect = SpriteEffects.FlipHorizontally;
			}

			Color col = Lighting.GetColor((int)(npc.Center.X / 16f), (int)(npc.Center.Y / 16f));
			spriteBatch.Draw(Main.npcTexture[npc.type], npc.Center - Main.screenPosition, npc.frame, col, realRot, npc.frame.Size() / 2f, 1f, effect, 0f);
			return false;
		}

		internal const float THREADGROWLERP = 30; //How many frames does it take for threads to fade in/out?

		private void DrawThreads(SpriteBatch spriteBatch)
		{
			if (threads.Count == 0 || attack != AttackType.StarlightConstellation)
				return;

			Texture2D tex = SpiritMod.Instance.GetTexture("Textures/Trails/Trail_4");
			Vector2 threadScale = new Vector2(1 / (float)tex.Width, 30 / (float)tex.Height); //Base scale of the thread based on the texture's size, stretched horizontally depending on thread length
			foreach (StarThread thread in threads)
			{
				float length = Math.Min(thread.Counter / THREADGROWLERP, (thread.Duration - thread.Counter) / THREADGROWLERP);
				length = Math.Min(length, 1);
				spriteBatch.Draw(tex, thread.StartPoint - Main.screenPosition, null, Color.SteelBlue * length, (thread.EndPoint - thread.StartPoint).ToRotation(), new Vector2(0f, tex.Height / 2),
					threadScale * new Vector2(thread.Length, 1), SpriteEffects.None, 0f);
			}
		}
		#endregion

		public enum AttackType : int
		{
			None = 0,
			Reset = 1,
			TeleportSlash = 2,
			Cast = 3,
			StarlightConstellation = 4,
			StarlightShootingStar = 5,
			MeteorDash = 6,
		}

		public void SetupRandomAttack()
		{
			var choices = new WeightedRandom<AttackType>(); //These two are always options
			choices.Add(AttackType.TeleportSlash, 1f);
			choices.Add(AttackType.Cast, 1f);

			if (enchantment == Enchantment.Starlight)
			{
				choices.Add(AttackType.StarlightConstellation, 0.5f);
				choices.Add(AttackType.StarlightShootingStar, 1.25f);
			}
			else if (enchantment == Enchantment.Meteor)
			{
				choices.Add(AttackType.MeteorDash, 1.5f);
			}

			attack = AttackType.StarlightConstellation;

			if (attack == AttackType.TeleportSlash)
			{
				attackTimeMax = 160; //Default slash time
				if (enchantment == Enchantment.Meteor)
					attackTimeMax = 200; //Meteor slash is slower
			}
			else if (attack == AttackType.Cast)
			{
				attackTimeMax = 80; //Starlight cast time
				if (enchantment == Enchantment.Meteor)
					attackTimeMax = 100; //Meteor cast
				else if (enchantment == Enchantment.Void)
					attackTimeMax = 200; //Void cast
			}
			else if (attack == AttackType.StarlightConstellation)
				attackTimeMax = 200;
			else if (attack == AttackType.StarlightShootingStar)
				attackTimeMax = Main.rand.Next(80, 90);
			else if (attack == AttackType.MeteorDash)
				attackTimeMax = 200;
		}

		public enum Enchantment : int
		{
			None = 0,
			Starlight = 1,
			Meteor = 2,
			Void = 3,
			Count = 4
		}

		public void SetRandomEnchantment() => enchantment = Enchantment.Starlight;// (Enchantment)(Main.rand.Next((int)Enchantment.Count - 1) + 1);

		internal void ResetEnchantment()
		{
			enchantment = Enchantment.None;
		}
	}
}
