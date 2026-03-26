using Fusion;

namespace SigilWarAscend.Gameplay
{
	public interface ISigilDamageable
	{
		bool IsAlive { get; }
		void TakeDamage(int damage, PlayerRef damageDealer = default);
	}
}
