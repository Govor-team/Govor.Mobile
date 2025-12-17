using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class MaxDescriptionLengthProvider : IMaxDescriptionLengthProvider
{
    public int MaxDescriptionLength => 500;
}