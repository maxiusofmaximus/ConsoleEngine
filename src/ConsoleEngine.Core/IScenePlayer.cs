namespace ConsoleEngine.Core;

/// <summary>
/// Abstraction for anything that can play a scene or sequence of scenes.
/// Implemented by <c>ScenePlayer</c> (single scene) and <c>SceneSequencer</c> (ordered sequence).
/// </summary>
public interface IScenePlayer
{
    /// <summary>Plays the scene or sequence and blocks until it completes.</summary>
    void Play();
}
