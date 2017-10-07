using System;

namespace Emotionic
{
    public enum Gesture
    {
        Jump,
        Punch,
        Specium,
        Daisuke,
        ChimpanzeeClap,
        Kamehameha,
        Always
    }

    public enum Effect
    {
        Line, /* Trail (ラインエフェクト) */
        Impact, /* StairBroken */
        Beam, /* KamehameCharge */
        Ripple, /* jump_and_clap_ripple (拍手) */
        Clap, /* clap_effe */
    }
}