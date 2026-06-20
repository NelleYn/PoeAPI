// Partial extension that restores a nested type missing from the modernized source.
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore.Shared;
partial class PluginManager
{
    internal record NotificationId
    {
        [CompilerGenerated]
        protected virtual Type EqualityContract
        {
            [CompilerGenerated]
            get
            {
                return (Type)typeof(NotificationId).TypeHandle;
            }
        }

        public string PluginId
        {
            [CompilerGenerated]
            get
            {
                return (string)(object)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public string Category
        {
            [CompilerGenerated]
            get
            {
                return (string)(object)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public string Notification
        {
            [CompilerGenerated]
            get
            {
                return (string)(object)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public NotificationId(string PluginId, string Category, string Notification)
        {
        }

        [CompilerGenerated]
        public override string ToString()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            return true;
        }

        [CompilerGenerated]
        public override int GetHashCode()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public virtual bool Equals(NotificationId? other)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        protected NotificationId(NotificationId original)
        {
        }

        [CompilerGenerated]
        public void Deconstruct(out string PluginId, out string Category, out string Notification)
        {
            PluginId = (string)(object)this;
            Category = (string)(object)this;
            Notification = (string)(object)this;
        }
    }
}