namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Identifies a game state managed by the client's game state controller.
/// </summary>
public enum GameStateTypes
{
    AreaLoadingState = 0,
    WaitingState = 1,
    CreditsState = 2,
    EscapeState = 3,
    InGameState = 4,
    ChangePasswordState = 5,
    LoginState = 6,
    PreGameState = 7,
    CreateCharacterState = 8,
    SelectCharacterState = 9,
    DeleteCharacterState = 10,
    LoadingState = 11,
    GameNotLoaded = 12
}
