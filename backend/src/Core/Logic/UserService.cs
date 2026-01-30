using Core.State;

namespace Core.Logic;

public record UserService(
    UserState state,
    //command
    DateTime now
    );