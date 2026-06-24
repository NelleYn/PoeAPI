namespace ExileCore.PoEMemory.Elements;
public class ChatPanel : Element
{
    public Element ChatTitlePanel => this;
    public Element ChatInputElement => this;
    public PoeChatElement ChatBox => (PoeChatElement)(object)this;
    public string InputText => (string)(object)this;
}