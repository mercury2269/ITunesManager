using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using System.Collections;
using iTunesLib;

namespace iTunesManager
{
    public partial class Form1 : Form
    {
        private volatile bool _shouldStop;
        private Thread worker;

        public Form1()
        {
            InitializeComponent();
        }

        private void RemoveDuplicates()
        {
            //create a reference to iTunes
            iTunesAppClass iTunes = new iTunesAppClass();

            //get a reference to the collection of all tracks
            IITTrackCollection tracks = iTunes.LibraryPlaylist.Tracks;

            int trackCount = tracks.Count;
            int numberChecked = 0;
            int numberDuplicateFound = 0;
            Dictionary<string, IITTrack> trackCollection = new Dictionary<string, IITTrack>();
            ArrayList tracksToRemove = new ArrayList();

            //setup the progress control
            this.SetupProgress(trackCount);

            for (int i = trackCount; i > 0; i--)
            {
                if (tracks[i].Kind == ITTrackKind.ITTrackKindFile)
                {
                    if (!this._shouldStop)
                    {
                        numberChecked++;
                        this.IncrementProgress();
                        this.UpdateLabel("Checking track # " + numberChecked.ToString() + " - " + tracks[i].Name);
                        string trackKey = tracks[i].Name + tracks[i].Artist + tracks[i].Album;

                        if (!trackCollection.ContainsKey(trackKey))
                        {
                            trackCollection.Add(trackKey, tracks[i]);
                        }
                        else
                        {
                            if (trackCollection[trackKey].Album != tracks[i].Album || trackCollection[trackKey].Artist != tracks[i].Artist)
                            {
                                trackCollection.Add(trackKey, tracks[i]);
                            }
                            else if (trackCollection[trackKey].BitRate > tracks[i].BitRate)
                            {
                                IITFileOrCDTrack fileTrack = (IITFileOrCDTrack)tracks[i];
                                numberDuplicateFound++;
                                tracksToRemove.Add(tracks[i]);
                            }
                            else
                            {
                                IITFileOrCDTrack fileTrack = (IITFileOrCDTrack)tracks[i];
                                trackCollection[trackKey] = fileTrack;
                                numberDuplicateFound++;
                                tracksToRemove.Add(tracks[i]);
                            }                            
                        }
                    }
                }                                
            }

            this.SetupProgress(tracksToRemove.Count);

            for (int i = 0; i < tracksToRemove.Count; i++)
            {
                IITFileOrCDTrack track = (IITFileOrCDTrack)tracksToRemove[i];
                this.UpdateLabel("Removing " + track.Name);
                this.IncrementProgress();
                this.AddTrackToList((IITFileOrCDTrack)tracksToRemove[i]);

                if (this.checkBoxRemove.Checked)
                {
                    track.Delete();
                }
            }

            this.UpdateLabel("Checked " + numberChecked.ToString() + " tracks and " + numberDuplicateFound.ToString() + " duplicate tracks found.");
            this.SetupProgress(1);
        }

        private void FindDeadTracks()
        {
            //create a reference to iTunes
            iTunesAppClass iTunes = new iTunesAppClass();

            //get a reference to the collection of all tracks
            IITTrackCollection tracks = iTunes.LibraryPlaylist.Tracks;

            int trackCount = tracks.Count;
            int numberChecked = 0;
            int numberDeadFound = 0;

            //setup the progress control
            this.SetupProgress(trackCount);

            for (int i = trackCount; i > 0; i--)
            {
                if (!this._shouldStop)
                {
                    IITTrack track = tracks[i];
                    numberChecked++;
                    this.IncrementProgress();
                    this.UpdateLabel("Checking track # " + numberChecked.ToString() + " - " + track.Name);
                    
                    if (track.Kind == ITTrackKind.ITTrackKindFile)
                    {
                        IITFileOrCDTrack fileTrack = (IITFileOrCDTrack)track;                        

                        //if the file doesn't exist, we'll delete it from iTunes
                        if (fileTrack.Location == String.Empty)
                        {
                            numberDeadFound++;
                            this.AddTrackToList(fileTrack);

                            if (this.checkBoxRemove.Checked)
                            {
                                fileTrack.Delete();
                            }
                        }
                        else if (!System.IO.File.Exists(fileTrack.Location))
                        {
                            numberDeadFound++;
                            this.AddTrackToList(fileTrack);

                            if (this.checkBoxRemove.Checked)
                            {
                                fileTrack.Delete();
                            }
                        }
                    }
                }
            }

            this.UpdateLabel("Checked " + numberChecked.ToString() + " tracks and " + numberDeadFound.ToString() + " dead tracks found.");
            this.SetupProgress(1);
        }

        private void ChangeTrackLocation()
        {
            //create a reference to iTunes
            var iTunes = new iTunesAppClass();

            //get a reference to the collection of all tracks
            IITTrackCollection tracks = iTunes.LibraryPlaylist.Tracks;

            int trackCount = tracks.Count;
            int numberChecked = 0;
            int numberRelocateFound = 0;

            //setup the progress control
            SetupProgress(trackCount);

            for (int i = trackCount; i > 0; i--)
            {
                if (!_shouldStop)
                {
                    IITTrack track = tracks[i];
                    numberChecked++;
                    IncrementProgress();
                    UpdateLabel("Relocating track # " + numberChecked.ToString(CultureInfo.InvariantCulture) + " - " + track.Name);

                    if (track.Kind == ITTrackKind.ITTrackKindFile)
                    {
                        var fileTrack = (IITFileOrCDTrack)track;

                        //if the file matches our old location
                        if (fileTrack.Location != null && fileTrack.Location.StartsWith(txtLocationOld.Text))
                        {
                            //check if file exists in the new location
                            var newLocation = fileTrack.Location.Replace(txtLocationOld.Text, txtLocationNew.Text);
                            if (System.IO.File.Exists(newLocation))
                            {
                                numberRelocateFound++;
                                AddTrackToList(fileTrack);

                                if (checkBoxRelocate.Checked)
                                {
                                    fileTrack.Location = newLocation;
                                }
                            }
                            

                        }
                    }
                }
            }

            this.UpdateLabel("Checked " + numberChecked.ToString() + " tracks and " + numberRelocateFound.ToString() + " relocate tracks found.");
            this.SetupProgress(1);
        }

        #region Message Handlers
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this._shouldStop = true;
            this.buttonCancel.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.label1.Text = "";
            this.buttonCancel.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this._shouldStop = false;
            this.buttonCancel.Enabled = true;
            this.listView1.Items.Clear();

            this.worker = new Thread(this.FindDeadTracks);
            this.worker.Start();
        }
        #endregion

        #region Delegate Callbacks
        //delagates for thread-safe access to UI components
        delegate void SetupProgressCallback(int max);
        delegate void IncrementProgressCallback();
        delegate void UpdateLabelCallback(string text);
        delegate void CompleteOperationCallback(string message);
        delegate void AddTrackToListCallback(IITFileOrCDTrack fileTrack);

        private void IncrementProgress()
        {
            if (this.progressBar1.InvokeRequired)
            {
                IncrementProgressCallback cb = new IncrementProgressCallback(IncrementProgress);
                this.Invoke(cb, new object[] { });
            }
            else
            {
                this.progressBar1.PerformStep();
            }
        }

        private void UpdateLabel(string text)
        {
            if (this.label1.InvokeRequired)
            {
                UpdateLabelCallback cb = new UpdateLabelCallback(UpdateLabel);
                this.Invoke(cb, new object[] { text });
            }
            else
            {
                this.label1.Text = text;
            }
        }

        private void CompleteOperation(string message)
        {
            if (this.label1.InvokeRequired)
            {
                CompleteOperationCallback cb = new CompleteOperationCallback(CompleteOperation);
                this.Invoke(cb, new object[] { message });
            }
            else
            {
                this.label1.Text = message;
            }
        }

        private void AddTrackToList(IITFileOrCDTrack fileTrack)
        {
            if (this.listView1.InvokeRequired)
            {
                AddTrackToListCallback cb = new AddTrackToListCallback(AddTrackToList);
                this.Invoke(cb, new object[] { fileTrack });
            }
            else
            {
                this.listView1.Items.Add(new ListViewItem(new string[] { fileTrack.Name, fileTrack.Artist, fileTrack.Location, fileTrack.BitRate.ToString() }));
            }
        }

        private void SetupProgress(int max)
        {
            if (this.progressBar1.InvokeRequired)
            {
                SetupProgressCallback cb = new SetupProgressCallback(SetupProgress);
                this.Invoke(cb, new object[] { max });
            }
            else
            {
                this.progressBar1.Maximum = max;
                this.progressBar1.Minimum = 1;
                this.progressBar1.Step = 1;
                this.progressBar1.Value = 1;
            }
        }
        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            this._shouldStop = false;
            this.buttonCancel.Enabled = true;
            this.listView1.Items.Clear();

            this.worker = new Thread(this.RemoveDuplicates);
            this.worker.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this._shouldStop = false;
            this.buttonCancel.Enabled = true;
            this.listView1.Items.Clear();

            this.worker = new Thread(this.ChangeTrackLocation);
            this.worker.Start();
        }
    }
}
