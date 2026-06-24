using System.Collections.Generic;
using ExileCore.Shared.Cache;

namespace ExileCore.PoEMemory.Elements;
public class InvitesPanel : Element
{
    private string _tradeRequestNotificationText;
    private string _partyInviteNotificationText;
    private string _friendInviteNotificationText;
    private string _guildInviteNotificationText;
    private readonly FrameCache<List<InvitesPanelItem>> _invitesCache;
    public string TradeRequestNotificationText
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string PartyInviteNotificationText
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string FriendInviteNotificationText
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string GuildInviteNotificationText
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<InvitesPanelItem> Invites => (List<InvitesPanelItem>)(object)this;
}