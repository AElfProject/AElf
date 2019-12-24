using System.Threading.Tasks;
using Volo.Abp.Settings;

namespace AElf.Blockchains.BasicBaseChain
{
    public class ResourceTokenSettingValueProvider : SettingValueProvider
    {
        public override string Name => "G";

        public ResourceTokenSettingValueProvider(ISettingStore settingStore) : base(settingStore)
        {
        }

        public override Task<string> GetOrNullAsync(SettingDefinition setting)
        {
            throw new System.NotImplementedException();
        }
    }
}