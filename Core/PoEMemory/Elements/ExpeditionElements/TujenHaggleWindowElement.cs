namespace ExileCore.PoEMemory.Elements.ExpeditionElements;
public class TujenHaggleWindowElement : Element
{
    public string WindowTitle => (string)0;
    public Element HaggleTargetItem => (Element)1;
    public Element HaggleArtifactType => (Element)3;

    public int HaggleArtifactCurrentOfferAmount
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public ArtifactSliderElement ArtifactOfferSliderElement => (ArtifactSliderElement)5;
    public Element SameNewOfferIndicator => (Element)6;
    public Element ConfirmButton => (Element)7;
    public Element ExitWindowButton => (Element)8;
    public Element HaggleTargetItemTooltipElement => (Element)9;
}