using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SpiritMod.Items.Weapon.Returning
{
    public class ReachBoomerang : ModItem
    {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Briarheart Boomerang");
            Tooltip.SetDefault("Shoots out two boomerangs on use");
        }


        public override void SetDefaults() {
            item.damage = 11;
            item.melee = true;
            item.width = 40;
            item.height = 40;
            item.useTime = 30;
            item.useAnimation = 30;
            item.noUseGraphic = true;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.knockBack = 3;
            item.value = Terraria.Item.sellPrice(0, 0, 4, 0);
            item.rare = 2;
            item.shootSpeed = 12f;
            item.shoot = ModContent.ProjectileType<Projectiles.Returning.ReachBoomerang>();
            item.UseSound = SoundID.Item1;
            item.autoReuse = false;
        }
        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
            {
                Projectile.NewProjectile(position.X, position.Y, speedX + ((float)Main.rand.Next(-200, 200) / 100), speedY + ((float)Main.rand.Next(-200, 200) / 100), type, damage, knockBack, player.whoAmI, 0f, 0f);
            }
            return true;
        }
    }
}