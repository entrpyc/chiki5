using Chiki.Sim;

namespace Sim;

public class Track
{
    [Test]
    public void beat_times_from_bpm()
    {
        var track = TestContent.Track(bpm: 120, offsetMs: 0);

        int time = track.BeatMap.TimeAtBeat(4);

        Assert.That(time, Is.EqualTo(2000));
    }

    [Test]
    public void offset_shifts_all_beats()
    {
        var track = TestContent.Track(bpm: 120, offsetMs: 250);

        int beat0 = track.BeatMap.TimeAtBeat(0);
        int beat1 = track.BeatMap.TimeAtBeat(1);

        Assert.Multiple(() =>
        {
            Assert.That(beat0, Is.EqualTo(250));
            Assert.That(beat1, Is.EqualTo(750));
        });
    }

    [Test]
    public void tempo_change_shifts_later_beats()
    {
        var track = TestContent.Track(bpm: 120, changes: new TempoChange(8, 240));

        int time = track.BeatMap.TimeAtBeat(12);

        Assert.That(time, Is.EqualTo(5000));
    }

    [Test]
    public void quarter_beats_between_beats()
    {
        var track = TestContent.Track(bpm: 120);

        int time = track.BeatMap.TimeAtQb(11); // 2.75 beats

        Assert.That(time, Is.EqualTo(1375));
    }
}
