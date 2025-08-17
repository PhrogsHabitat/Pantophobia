namespace Phobia.Gameplay
{
    /// <summary>
    /// Interface for scenes that require PlayState initialization
    /// </summary>
    public interface IPlayStateInitializable
    {
        void Initialize(PlayState playState);
    }
}
