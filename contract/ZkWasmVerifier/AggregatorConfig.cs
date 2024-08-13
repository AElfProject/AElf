using System.Linq;
using Bn254.Net;
using Nethereum.Util;

namespace ZkWasmVerifier;

public static class AggregatorConfig
{
    
    public static void FillVerifyCircuitsG2(UInt256[] s)  {
        s[2] = "10912121346736960153119032326674308622836895172287017181004332853287395747540";
        s[3] = "1141242303575873671169773919745529817497021056319105990006251901769180589131";
        s[4] = "13736722489223410979012950988289654946078517441362861355057236908979399480985";
        s[5] = "17920167001006791983741102148022475408561884350390649459550129912018198675534";

        s[8] = "11559732032986387107991004021392285783925812861821192530917403151452391805634";
        s[9] = "10857046999023057135944570762232829481370756359578518086990519993285655852781";
        s[10] = "17805874995975841540914202342111839520379459829704422454583296818431106115052";
        s[11] = "13392588948715843804641432497768002650278120570034223513918757245338268106653";
    }
    public static void GetChallenges(UInt256[] transcript, UInt256[] buf)
    {
        GetChallengesShplonk(transcript, buf);
    }

    public static void GetChallengesShplonk(UInt256[] transcript, UInt256[] buf)
    {
        UInt256[] absorbing = new UInt256[112];
        absorbing[0] = "8987513744584090369347489657311893833926946877426413758008060670913747976065";
        absorbing[1] = buf[0];
        absorbing[2] = buf[1];
        var pos = 3;
        var transcriptPos = 0;
        for (var i = 0; i < 8; i++)
        {
            AggregatorLib.CheckOnCurve(transcript[transcriptPos], transcript[transcriptPos + 1]);
            absorbing[pos++] = transcript[transcriptPos++];
            absorbing[pos++] = transcript[transcriptPos++];
        }

        // theta        
        buf[2] = SqeezeChallenge(absorbing, pos);

        pos = 1;
        for (var i = 0; i < 4; i++)
        {
            AggregatorLib.CheckOnCurve(transcript[transcriptPos], transcript[transcriptPos + 1]);
            absorbing[pos++] = transcript[transcriptPos++];
            absorbing[pos++] = transcript[transcriptPos++];
        }

        // beta
        buf[3] = SqeezeChallenge(absorbing, pos);

        pos = 1;
        // gamma
        buf[4] = SqeezeChallenge(absorbing, pos);

        pos = 1;
        for (int i = 0; i < 7; i++)
        {
            AggregatorLib.CheckOnCurve(transcript[transcriptPos], transcript[transcriptPos + 1]);
            absorbing[pos++] = transcript[transcriptPos++];
            absorbing[pos++] = transcript[transcriptPos++];
        }

        // y
        buf[5] = SqeezeChallenge(absorbing, pos);

        pos = 1;
        for (int i = 0; i < 3; i++)
        {
            AggregatorLib.CheckOnCurve(transcript[transcriptPos], transcript[transcriptPos + 1]);
            absorbing[pos++] = transcript[transcriptPos++];
            absorbing[pos++] = transcript[transcriptPos++];
        }

        // x
        buf[6] = SqeezeChallenge(absorbing, pos);

        pos = 1;
        for (var i = 0; i < 56; i++)
        {
            absorbing[pos++] = transcript[transcriptPos++];
        }

        // y
        buf[7] = SqeezeChallenge(absorbing, pos);

        pos = 1;
        // v
        buf[8] = SqeezeChallenge(absorbing, pos);

        AggregatorLib.CheckOnCurve(transcript[transcriptPos], transcript[transcriptPos + 1]);
        absorbing[pos++] = transcript[transcriptPos++];
        absorbing[pos++] = transcript[transcriptPos++];

        // u
        buf[9] = SqeezeChallenge(absorbing, pos);
        AggregatorLib.CheckOnCurve(transcript[transcriptPos], transcript[transcriptPos + 1]);
    }

    public static void CalcVerifyCircuitLagrange(UInt256[] buf)
    {
        buf[0] = "2735708597799451452160332461848350692128893288992167946537450125562183732533";
        buf[1] = "16512170175385812892195391099574916023024416995263347775961451770318731872745";
        AggregatorLib.Msm(buf, 0, 1);
    }

    public static UInt256 Hash(UInt256[] absorbing, UInt256 length)
    {
        var bytes = absorbing.Take(length).SelectMany(x => x.ToBigEndianBytes()).Concat(new byte[1]).ToArray();
        var hash = Sha3Keccack.Current.CalculateHash(bytes);
        return hash.ToUInt256();
    }

    public static UInt256 SqeezeChallenge(UInt256[] absorbing, UInt256 length)
    {
        absorbing[length] = 0;
        var hash = Hash(absorbing, length);
        absorbing[0] = hash;
        return hash % AggregatorLib.QMod;
    }
}