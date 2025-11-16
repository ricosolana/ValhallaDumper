using Jotunn.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValhallaDumper
{
    public class AvlDumpCommand : ConsoleCommand
    {
        public override string Name => "avl_dump";

        public override string Help => "dump data for use with Avledet server";

        public override void Run(string[] args)
        {
            // dump all

            if (args.Length == 0)
            {
                //Chat.print("This will lagg your game a lot during dumping");
                //Chat.print("Dumping will also destroy the current world.");
                //Chat.print("USE WITH CAUTION!");
                //
                //Chat.print("run this command again with 'confirm'");

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Run with /avl_dump confirm");
                return;
            }

            try
            {
                Dumper.DumpDocs();
                Dumper.DumpPackages();

                //MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Run with /avl_dump confirm");
                //Chat.print("Dumping success");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.instance.m_sessionID, "ChatMessage", new object[]
                {
                        Player.m_localPlayer.GetHeadPoint(),
                        (int)Talker.Type.Normal,
                        UserInfo.GetLocalUser(),
                        "Dumping success"
                });
            }
            catch (Exception ex)
            {
                //Chat.print("Dumping exception, see console");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.instance.m_sessionID, "ChatMessage", new object[]
                {
                        Player.m_localPlayer.GetHeadPoint(),
                        (int)Talker.Type.Normal,
                        UserInfo.GetLocalUser(),
                        "Dumping failed; see logs"
                });
                throw new Exception("Exception: ", ex);
            }
        }
    }
}
