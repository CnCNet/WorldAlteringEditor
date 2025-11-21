using System;
using System.IO;
using System.Windows.Forms;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class CopiedTriggerData
    {
        public TriggerCondition CopiedTriggerEvent = null;
        public TriggerAction CopiedTriggerAction = null;

        public void CopyToClipboard()
        {
            if (CopiedTriggerEvent == null && CopiedTriggerAction == null)
                return;

            using var memoryStream = new MemoryStream();

            if (CopiedTriggerEvent == null)
            {
                memoryStream.WriteByte(0);
            }
            else
            {
                memoryStream.WriteByte(1);
                CopiedTriggerEvent.Serialize(memoryStream);
            }

            if (CopiedTriggerAction == null)
            {
                memoryStream.WriteByte(0);
            }
            else
            {
                memoryStream.WriteByte(1);
                CopiedTriggerAction.Serialize(memoryStream);
            }

            byte[] bytes = memoryStream.ToArray();
            Clipboard.SetData(Constants.ClipboardTriggerActionEventFormatValue, bytes);
        }

        public TriggerAction GetTriggerActionFromClipboard()
        {
            if (!Clipboard.ContainsData(Constants.ClipboardTriggerActionEventFormatValue))
                return null;

            var bytes = (byte[])Clipboard.GetData(Constants.ClipboardTriggerActionEventFormatValue);

            using var memoryStream = new MemoryStream(bytes);

            SkipTriggerEventDataInStream(memoryStream);

            int hasTriggerAction = memoryStream.ReadByte();
            if (hasTriggerAction <= 0)
                return null;

            var triggerAction = new TriggerAction();
            triggerAction.Deserialize(memoryStream);

            return triggerAction;
        }

        public TriggerCondition GetTriggerEventFromClipboard()
        {
            if (!Clipboard.ContainsData(Constants.ClipboardTriggerActionEventFormatValue))
                return null;

            var bytes = (byte[])Clipboard.GetData(Constants.ClipboardTriggerActionEventFormatValue);

            using var memoryStream = new MemoryStream(bytes);
            int hasTriggerEvent = memoryStream.ReadByte();
            if (hasTriggerEvent <= 0)
                return null;

            var triggerEvent = new TriggerCondition();
            triggerEvent.Deserialize(memoryStream);

            return triggerEvent;
        }

        private void SkipTriggerEventDataInStream(MemoryStream memoryStream)
        {
            int hasTriggerEvent = memoryStream.ReadByte();
            if (hasTriggerEvent <= 0)
                return;
            
            memoryStream.Position += 4; // skip condition index

            Span<byte> buffer = stackalloc byte[4];
            for (int i = 0; i < TriggerCondition.MAX_PARAM_COUNT; i++) // skip params
            {
                memoryStream.Read(buffer);
                int stringLength = BitConverter.ToInt32(buffer);
                if (stringLength <= 0) 
                    continue;

                memoryStream.Position += stringLength;
            }
        }

        public bool HasTriggerActionDataInClipboard()
        {
            return HasTriggerActionOrEventData(true);
        }

        public bool HasTriggerEventDataInClipboard()
        {
            return HasTriggerActionOrEventData(false);
        }

        private bool HasTriggerActionOrEventData(bool skipEvent)
        {
            if (!Clipboard.ContainsData(Constants.ClipboardTriggerActionEventFormatValue))
                return false;

            byte[] bytes = (byte[])Clipboard.GetData(Constants.ClipboardTriggerActionEventFormatValue);

            using var memoryStream = new MemoryStream(bytes);

            if (skipEvent)
                SkipTriggerEventDataInStream(memoryStream);

            int hasTriggerEventOrActionFlag = memoryStream.ReadByte();
            return hasTriggerEventOrActionFlag == 1;
        }

        public void ClearValuesIfClipboardEmpty()
        {
            if (Clipboard.ContainsData(Constants.ClipboardTriggerActionEventFormatValue))
                return;

            CopiedTriggerAction = null;
            CopiedTriggerEvent = null;
        }

        public void SetCopiedTriggerEvent(TriggerCondition triggerEvent)
        {
            if (triggerEvent == null)
                return;

            ClearValuesIfClipboardEmpty();
            CopiedTriggerEvent = triggerEvent;
        }

        public void SetCopiedTriggerAction(TriggerAction triggerAction)
        {
            if (triggerAction == null)
                return;

            ClearValuesIfClipboardEmpty();
            CopiedTriggerAction = triggerAction;
        }
    }    
}
