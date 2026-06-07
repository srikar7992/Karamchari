using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Geo;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public sealed class GeoSignalFusionTests
{
    [Fact]
    public void Perfect_gps_and_wifi_and_ip_scores_above_70()
    {
        // GPS(35) + WiFi(25) + IP(20) = 80 >= 70 → Accepted
        var result = GeoSignalFusion.Compute(
            gpsInsideZone: true,
            gpsAccuracyMeters: 5,
            wifiMatched: true,
            ipWhitelisted: true,
            bleBeaconDetected: false,
            vpnMatched: false);

        result.TotalScore.Should().BeGreaterThanOrEqualTo(70);
        result.Outcome.Should().Be(GeoValidationOutcome.Accepted);
        result.IsAccepted.Should().BeTrue();
    }

    [Fact]
    public void No_signals_at_all_scores_zero_and_rejected()
    {
        var result = GeoSignalFusion.NoSignal();

        result.TotalScore.Should().Be(0);
        result.Outcome.Should().Be(GeoValidationOutcome.Rejected);
        result.IsRejected.Should().BeTrue();
    }

    [Fact]
    public void Ip_whitelist_only_scores_20_and_is_rejected()
    {
        // 20 < 40 threshold → Rejected
        var result = GeoSignalFusion.Compute(
            gpsInsideZone: false,
            gpsAccuracyMeters: null,
            wifiMatched: false,
            ipWhitelisted: true,
            bleBeaconDetected: false,
            vpnMatched: false);

        result.TotalScore.Should().Be(20);
        result.Outcome.Should().Be(GeoValidationOutcome.Rejected);
        result.IsRejected.Should().BeTrue();
    }

    [Fact]
    public void All_signals_present_scores_100_max()
    {
        var result = GeoSignalFusion.Compute(
            gpsInsideZone: true,
            gpsAccuracyMeters: 5,
            wifiMatched: true,
            ipWhitelisted: true,
            bleBeaconDetected: true,
            vpnMatched: true);

        // GPS(35) + WiFi(25) + IP(20) + BLE(15) + VPN(5) = 100
        result.TotalScore.Should().Be(100);
        result.IsAccepted.Should().BeTrue();
    }

    [Fact]
    public void Poor_gps_accuracy_reduces_gps_points()
    {
        var goodAccuracy = GeoSignalFusion.Compute(true, 5, false, false, false, false);
        var badAccuracy = GeoSignalFusion.Compute(true, 200, false, false, false, false);

        goodAccuracy.GpsPoints.Should().BeGreaterThan(badAccuracy.GpsPoints);
    }

    [Theory]
    [InlineData(true, true, false, false, false, GeoValidationOutcome.SoftFlag)]    // GPS(35)+WiFi(25)=60 → SoftFlag (40-69)
    [InlineData(false, false, true, true, false, GeoValidationOutcome.Rejected)]   // IP(20)+BLE(15)=35 → Rejected (<40)
    [InlineData(false, true, true, false, false, GeoValidationOutcome.SoftFlag)]   // WiFi(25)+IP(20)=45 → SoftFlag
    [InlineData(false, false, false, false, false, GeoValidationOutcome.Rejected)] // nothing → Rejected
    public void Score_boundaries_produce_correct_outcomes(
        bool gps, bool wifi, bool ip, bool ble, bool vpn, GeoValidationOutcome expected)
    {
        var result = GeoSignalFusion.Compute(gps, gps ? 5.0 : null, wifi, ip, ble, vpn);
        result.Outcome.Should().Be(expected);
    }
}
