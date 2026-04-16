namespace ChatarPatar.API.Attributes;

internal sealed class ResponseWrapperAttribute : Attribute
{
    public bool WrapResponse { get; set; } = true;
}
