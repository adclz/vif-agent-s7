using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;

namespace Vif_siemens_compiler.Siemens;

public class Hw
{
    private static PlcSoftware GetPlcSoftware(HardwareObject device) =>
        device.DeviceItems
            .Where(item => item.GetService<SoftwareContainer>() != null)
            .Select(item => item.GetService<SoftwareContainer>().Software)
            .DefaultIfEmpty()
            .First() as PlcSoftware;
    
    public static List<PlcSoftware> ListPlc(TiaPortal process)
    {
        return process.Projects[0].Devices.Select(GetPlcSoftware).ToList();
    }
}