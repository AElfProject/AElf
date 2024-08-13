using Bn254.Net;

namespace ZkWasmVerifier;

public class AggregatorVerifierCoreStep3 : IAggregatorVerifierCoreStep
{
    public UInt256[] VerifyProof(UInt256[] transcript, UInt256[] aux, UInt256[] buf)
    {
        // @formatter:off
        var mulmod = (UInt256 a, UInt256 b, UInt256 m) => (a * b) % m;
        var addmod = (UInt256 a, UInt256 b, UInt256 m) => (a + b) % m;
        (buf[14], buf[15]) = (transcript[40], transcript[41]);
        buf[16] = mulmod(buf[17], buf[35], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = (transcript[38], transcript[39]);
        buf[16] = buf[17];
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = (buf[0], buf[1]);
        buf[16] = mulmod(buf[21], mulmod(buf[30], buf[7], AggregatorLib.QMod.ToUInt256()), AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        buf[17] = mulmod(mulmod(buf[19], buf[8], AggregatorLib.QMod.ToUInt256()), buf[28], AggregatorLib.QMod.ToUInt256());
        (buf[14], buf[15]) = (transcript[16], transcript[17]);
        buf[16] = mulmod(buf[17], buf[7], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        buf[20] = mulmod(buf[20], buf[23], AggregatorLib.QMod.ToUInt256());
        (buf[14], buf[15]) = (transcript[18], transcript[19]);
        buf[16] = mulmod(buf[21], mulmod(buf[20], buf[7], AggregatorLib.QMod.ToUInt256()), AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = (transcript[32], transcript[33]);
        buf[16] = mulmod(buf[25], buf[7], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = (transcript[20], transcript[21]);
        buf[16] = buf[17];
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = (transcript[22], transcript[23]);
        buf[16] = mulmod(buf[21], buf[20], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = (transcript[34], transcript[35]);
        buf[16] = buf[25];
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = ("13292207742358048193363903155311282303694990999153147378260744839175179844272", "8069646990961401522004619706042590276391180087782205971002027097172883400355");
        buf[16] = mulmod(buf[21], buf[26], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        buf[17] = mulmod(buf[24], buf[23], AggregatorLib.QMod.ToUInt256());
        (buf[14], buf[15]) = ("16962709593911359056643584098658643393752005484223049643104296353822683710608", "10559423757078030678373057222882755533496505850657344541786773871445981479972");
        buf[16] = mulmod(buf[21], mulmod(buf[17], buf[7], AggregatorLib.QMod.ToUInt256()), AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = ("1289930964916810258948649523842583935162163198616891195090185552481132662306", "9349427207481746847457819049174580746099595602688060645669692066778067157287");
        buf[16] = mulmod(buf[21], buf[17], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = ("13027342631838746326863822286082389694409544498995811462671038670568858108891", "5047939170640162610161164136085743909314737380382289915607715473538237141482");
        buf[16] = mulmod(buf[21], mulmod(buf[24], buf[7], AggregatorLib.QMod.ToUInt256()), AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = ("4202332094118111758910265968032668730009038512388316357931820855479441638624", "14227708779289885609649995391374967451355616790242748385077714572462384655123");
        buf[16] = mulmod(buf[21], buf[24], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = ("9230882410532336358822556144187239116196302704399492952375596356113985875521", "13682064521086611398833038852259806568259918019163169600724810995458152518040");
        buf[16] = mulmod(buf[21], buf[31], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = ("3410909432599015228817777153842470803679052023504007951748169347603737115779", "4278355855479791189030463433364613592289197199970447328585000204791248828073");
        buf[16] = mulmod(buf[21], buf[23], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        buf[17] = mulmod(buf[19], buf[19], AggregatorLib.QMod.ToUInt256());
        (buf[14], buf[15]) = (transcript[24], transcript[25]);
        buf[16] = mulmod(buf[17], buf[23], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = (transcript[26], transcript[27]);
        buf[16] = mulmod(buf[17], buf[7], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = (transcript[28], transcript[29]);
        buf[16] = buf[17];
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = (transcript[30], transcript[31]);
        buf[16] = mulmod(buf[25], buf[23], AggregatorLib.QMod.ToUInt256());
        AggregatorLib.EccMulAdd(buf, 12);
        (buf[14], buf[15]) = (transcript[36], transcript[37]);
        buf[16] = buf[21];
        AggregatorLib.EccMulAdd(buf, 12);

        UInt256[] ret = new UInt256[4];
        ret[0] = buf[10];
        ret[1] = buf[11];
        ret[2] = buf[12];
        ret[3] = buf[13];
        // @formatter:on
        return ret;
    }
}