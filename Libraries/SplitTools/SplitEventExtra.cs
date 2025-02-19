﻿using FraGag.Compression;
using Newtonsoft.Json;
using SAModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SplitTools.SAArc
{
	public static class sa2EventExtra
	{
		public static void Split(string filename, string outputPath)
		{
			string dir = Environment.CurrentDirectory;
			try
			{
				if (outputPath[outputPath.Length - 1] != '/') outputPath = string.Concat(outputPath, "/");
				// get file name, read it from the console if nothing
				string evfilename = filename;

				evfilename = Path.GetFullPath(evfilename);
				Console.WriteLine("Splitting file {0}...", filename);
				byte[] fc;
				if (Path.GetExtension(filename).Equals(".prs", StringComparison.OrdinalIgnoreCase))
					fc = Prs.Decompress(filename);
				else
					fc = File.ReadAllBytes(filename);
				EventExtraIniData ini = new EventExtraIniData() { Name = Path.GetFileNameWithoutExtension(filename) };
				if (outputPath.Length != 0)
				{
					if (!Directory.Exists(outputPath))
						Directory.CreateDirectory(outputPath);
					Environment.CurrentDirectory = outputPath;
				}
				else
					Environment.CurrentDirectory = Path.GetDirectoryName(evfilename);
				Directory.CreateDirectory(Path.GetFileNameWithoutExtension(evfilename));
				bool battle;
				bool beta;
				bool lang;
				if (fc.Length == 0x2DC00)
				{
					Console.WriteLine("File is in DC Beta format.");
					ByteConverter.BigEndian = false;
					ini.Game = Game.SA2;
					battle = false;
					beta = true;
					ini.BigEndian = false;
				}
				else if (fc[4] > 0 || fc[8] > 0 || fc[0x800] > 0 || (fc.Length > 0x9900 && fc[0x26800] > 0))
				{ 
					Console.WriteLine("File is in DC format.");
					ByteConverter.BigEndian = false;
					ini.Game = Game.SA2;
					battle = false;
					beta = false;
					ini.BigEndian = false;
				}
				else
				{
					Console.WriteLine("File is in GC/PC format.");
					ByteConverter.BigEndian = true;
					ini.Game = Game.SA2B;
					battle = true;
					beta = false;
					ini.BigEndian = true;
				}
				if (fc.Length < 0x9900)
				{
					lang = true;
					ini.LanguageOnly = true;
				}
				else
				{
					lang = false;
					ini.LanguageOnly = false;
				}
				if (lang)
					Console.WriteLine("File only contains audio/subtitle timings.");
				int address = 0;
				int subcount = 0;
				for (int i = 0; i < 256; i++)
				{
					address = 0x8 * i;
					SubtitleInfo subs = new SubtitleInfo();
					subs.FrameStart = ByteConverter.ToUInt32(fc, address);
					if (subs.FrameStart != 0)
						subcount++;
					subs.VisibleTime = ByteConverter.ToUInt32(fc, address + 4);
					ini.Subtitles.Add(subs);
				}
				if (subcount != 0)
					Console.WriteLine("Event contains {0} active subtitle entr{1}.", subcount, subcount == 1 ? "y" : "ies");
				else
					Console.WriteLine("Event does not use subtitles.");

				int audiocount = 0;
				for (int i = 0; i < 512; i++)
				{
					address = 0x800 + (0x48 * i);
					AudioInfo audio = new AudioInfo();
					audio.FrameStart = ByteConverter.ToUInt32(fc, address);
					if (audio.FrameStart != 0)
						audiocount++;
					audio.VoiceEntry1 = ByteConverter.ToInt16(fc, address + 4).ToCHex();
					audio.VoiceEntry2 = ByteConverter.ToInt16(fc, address + 6).ToCHex();
					audio.MusicEntry = fc.GetCString(address + 8);
					ini.AudioInfo.Add(audio);
				}
				if (audiocount != 0)
					Console.WriteLine("Event contains {0} active audio entr{1}.", audiocount, audiocount == 1 ? "y" : "ies");
				else
					Console.WriteLine("Event does not contain active audio entries.");

				if (fc.Length > 0x9900)
				{
					int screencount = 0;
					for (int i = 0; i < 64; i++)
					{
						address = 0x9800 + (0x40 * i);
						ScreenEffects screen = new ScreenEffects();
						screen.FrameStart = ByteConverter.ToUInt32(fc, address);
						if (screen.FrameStart != 0)
							screencount++;
						screen.Type = fc[address + 4];
						if (battle)
						{
							screen.A = fc[address + 8];
							screen.R = fc[address + 9];
							screen.G = fc[address + 0xA];
							screen.B = fc[address + 0xB];
						}
						else
						{
							screen.B = fc[address + 8];
							screen.G = fc[address + 9];
							screen.R = fc[address + 0xA];
							screen.A = fc[address + 0xB];
						}
						ini.ScreenEffects.Add(screen);
					}
					if (screencount != 0)
						Console.WriteLine("Event contains {0} active screen effect entr{1}.", screencount, screencount == 1 ? "y" : "ies");
					else
						Console.WriteLine("Event does not use screen effects.");

					int particlecount = 0;
					for (int i = 0; i < 2048; i++)
					{
						address = 0xA800 + (0x38 * i);
						ParticleEffects particle = new ParticleEffects();
						particle.FrameStart = ByteConverter.ToUInt32(fc, address);
						if (particle.FrameStart != 0)
							particlecount++;
						particle.Type1 = fc[address + 4];
						particle.Type2 = fc[address + 5];
						particle.Position = new Vertex(fc, address + 8);
						particle.Intensity = ByteConverter.ToSingle(fc, address + 0x14);
						ini.ParticleEffects.Add(particle);
					}
					if (particlecount != 0)
						Console.WriteLine("Event contains {0} active standard particle effect entr{1}.", particlecount, particlecount == 1 ? "y" : "ies");
					else
						Console.WriteLine("Event does not use standard particle effects.");

					int lightcount = 0;
					if (beta)
					{
						for (int i = 0; i < 436; i++)
						{
							address = 0x26800 + (0x44 * i);
							LightingInfo light = new LightingInfo();
							light.FrameStart = ByteConverter.ToUInt32(fc, address);
							if (light.FrameStart != 0)
								lightcount++;
							light.FadeType = ByteConverter.ToInt32(fc, address + 4);
							light.LightDirection = new Vertex(fc, address + 8);
							light.Color = new Vertex(fc, address + 0x14);
							light.Intensity = ByteConverter.ToSingle(fc, address + 0x20);
							light.AmbientColor = new Vertex(fc, address + 0x24);
							ini.Lighting.Add(light);
						}
						if (lightcount != 0)
							Console.WriteLine("Event contains {0} active lighting entr{1}.", lightcount, lightcount == 1 ? "y" : "ies");
						else
							Console.WriteLine("Event does not use lighting.");
						// Space of 0x30 following the data chunk. Investigate further.
					}
					else
					{
						for (int i = 0; i < 1084; i++)
						{
							address = 0x26800 + (0x44 * i);
							LightingInfo light = new LightingInfo();
							light.FrameStart = ByteConverter.ToUInt32(fc, address);
							if (light.FrameStart != 0)
								lightcount++;
							light.FadeType = ByteConverter.ToInt32(fc, address + 4);
							light.LightDirection = new Vertex(fc, address + 8);
							light.Color = new Vertex(fc, address + 0x14);
							light.Intensity = ByteConverter.ToSingle(fc, address + 0x20);
							light.AmbientColor = new Vertex(fc, address + 0x24);
							ini.Lighting.Add(light);
						}
						if (lightcount != 0)
							Console.WriteLine("Event contains {0} active lighting entr{1}.", lightcount, lightcount == 1 ? "y" : "ies");
						else
							Console.WriteLine("Event does not use lighting.");

						//add a buffer of 0x10 between these two chunks for build operations

						int particle2count = 0;
						for (int i = 0; i < 64; i++)
						{
							address = 0x38800 + (0x40 * i);
							ParticleEffects2 particle2 = new ParticleEffects2();
							particle2.Position = new Vertex(fc, address);
							particle2.Unk2 = new Vertex(fc, address + 0xC);
							particle2.Unk3 = ByteConverter.ToInt16(fc, address + 0x18);
							particle2.Unk4 = ByteConverter.ToInt16(fc, address + 0x1A);
							particle2.Unk5 = ByteConverter.ToInt16(fc, address + 0x1C);
							particle2.Unk6 = ByteConverter.ToInt16(fc, address + 0x1E);
							particle2.FrameStart = ByteConverter.ToUInt32(fc, address + 0x20);
							if (particle2.FrameStart != 0)
								particle2count++;
							particle2.Spread = new Vertex(fc, address + 0x24);
							particle2.Count = ByteConverter.ToInt32(fc, address + 0x30);
							particle2.Unk9 = ByteConverter.ToInt32(fc, address + 0x34);
							particle2.Type = ByteConverter.ToInt32(fc, address + 0x38);
							particle2.Unk11 = ByteConverter.ToInt32(fc, address + 0x3C);
							ini.ParticleEffects2.Add(particle2);
						}
						if (particle2count != 0)
							Console.WriteLine("Event contains {0} active animated particle effect entr{1}.", particle2count, particle2count == 1 ? "y" : "ies");
						else
							Console.WriteLine("Event does not use animated particle effects.");

						int videocount = 0;
						for (int i = 0; i < 64; i++)
						{
							address = 0x39800 + (0x40 * i);
							VideoInfo video = new VideoInfo();
							video.FrameStart = ByteConverter.ToUInt32(fc, address);
							if (video.FrameStart != 0)
								videocount++;
							video.VideoType = ByteConverter.ToUInt16(fc, address + 0x4);
							video.Unknown = ByteConverter.ToUInt16(fc, address + 0x6);
							video.Depth = ByteConverter.ToSingle(fc, address + 0x8);
							video.OverlayType = fc[address + 0xC];
							video.OverlayType2 = fc[address + 0xD];
							video.VideoName = fc.GetCString(address + 0x10);
							ini.VideoInfo.Add(video);
						}
						if (videocount != 0)
							Console.WriteLine("Event contains {0} active video entr{1}.", videocount, videocount == 1 ? "y" : "ies");
						else
							Console.WriteLine("Event does not use video effects.");
					}
				}
				JsonSerializer js = new JsonSerializer
				{
					Formatting = Formatting.Indented,
					NullValueHandling = NullValueHandling.Ignore
				};
				using (var tw = File.CreateText(Path.Combine(Path.GetFileNameWithoutExtension(evfilename), Path.ChangeExtension(Path.GetFileName(filename), ".json"))))
					js.Serialize(tw, ini);
			}
			finally
			{
				Environment.CurrentDirectory = dir;
			}
		}

		public static void SplitMini(string filename, string outputPath)
		{
			string dir = Environment.CurrentDirectory;
			try
			{
				if (outputPath[outputPath.Length - 1] != '/') outputPath = string.Concat(outputPath, "/");
				// get file name, read it from the console if nothing
				string evfilename = filename;

				evfilename = Path.GetFullPath(evfilename);
				Console.WriteLine("Splitting file {0}...", filename);
				byte[] fc;
				if (Path.GetExtension(filename).Equals(".prs", StringComparison.OrdinalIgnoreCase))
					fc = Prs.Decompress(filename);
				else
					fc = File.ReadAllBytes(filename);
				MiniEventExtraIniData ini = new MiniEventExtraIniData() { Name = Path.GetFileNameWithoutExtension(filename) };
				if (outputPath.Length != 0)
				{
					if (!Directory.Exists(outputPath))
						Directory.CreateDirectory(outputPath);
					Environment.CurrentDirectory = outputPath;
				}
				else
					Environment.CurrentDirectory = Path.GetDirectoryName(evfilename);
				Directory.CreateDirectory(Path.GetFileNameWithoutExtension(evfilename));
				if (fc[4] > 0 || fc[8] > 0 || fc[0x100] > 0)
				{
					Console.WriteLine("File is in DC format.");
					ByteConverter.BigEndian = false;
					ini.Game = Game.SA2;
					ini.BigEndian = false;
				}
				else
				{
					Console.WriteLine("File is in GC/PC format.");
					ByteConverter.BigEndian = true;
					ini.Game = Game.SA2B;
					ini.BigEndian = true;
				}
				int addr = 0;
				int subcount = 0;
				for (int i = 0; i < 32; i++)
				{
					addr = 0x8 * i;
					SubtitleInfo subs = new SubtitleInfo();
					subs.FrameStart = ByteConverter.ToUInt32(fc, addr);
					if (subs.FrameStart != 0)
						subcount++;
					subs.VisibleTime = ByteConverter.ToUInt32(fc, addr + 4);
					ini.Subtitles.Add(subs);
				}
				if (subcount != 0)
					Console.WriteLine("Mini-Event contains {0} active subtitle entr{1}.", subcount, subcount == 1 ? "y" : "ies");
				else
					Console.WriteLine("Mini-Event does not use subtitles.");

				int effectcount = 0;
				for (int i = 0; i < 64; i++)
				{
					addr = 0x100 + (0x4C * i);
					EffectInfo fx = new EffectInfo();
					int frame = fc.GetPointer(addr, 0);
					fx.FrameStart = ByteConverter.ToUInt32(fc, addr);
					if (frame != 0)
						effectcount++;
					fx.FadeType = fc[addr + 4];
					fx.SFXEntry1 = fc[addr + 5];
					fx.SFXEntry2 = fc[addr + 6];
					fx.VoiceEntry = ByteConverter.ToUInt16(fc, addr + 8).ToCHex();
					fx.MusicEntry = fc.GetCString(addr + 0xA);
					ini.Effects.Add(fx);
				}
				if (effectcount != 0)
					Console.WriteLine("Mini-Event contains {0} active effect entr{1}.", effectcount, effectcount == 1 ? "y" : "ies");
				else
					Console.WriteLine("Mini-Event does not use additional effects.");
				int misccount = 0;
				for (int i = 0; i < 1; i++)
				{
					addr = 0x1400;
					MiscMiniEffect misc = new MiscMiniEffect();
					int unkdata1 = fc.GetPointer(addr, 0);
					misc.Unk1 = new Vertex(fc, addr);
					misc.Unk2 = ByteConverter.ToSingle(fc, addr + 0xC);
					int unkdata2 = fc.GetPointer(addr + 0x10, 0);
					misc.Unk3 = new Vertex(fc, addr + 0x10);
					if (unkdata1 != 0 || unkdata2 != 0)
						misccount++;
					ini.Unknown.Add(misc);
				}
				if (misccount != 0)
					Console.WriteLine("Mini-Event contains an unknown effect entry.");
				else
					Console.WriteLine("Mini-Event does not use unknown effects.");

				JsonSerializer js = new JsonSerializer
				{
					Formatting = Formatting.Indented,
					NullValueHandling = NullValueHandling.Ignore
				};
				using (var tw = File.CreateText(Path.Combine(Path.GetFileNameWithoutExtension(evfilename), Path.ChangeExtension(Path.GetFileName(filename), ".json"))))
					js.Serialize(tw, ini);
			}
			finally
			{
				Environment.CurrentDirectory = dir;
			}
		}
		public static void Build(bool? isBigEndian, bool? isLanguageFile, string filename)
		{
			string dir = Environment.CurrentDirectory;
			try
			{
				byte[] fc;
				if (Path.GetExtension(filename).Equals(".prs", StringComparison.OrdinalIgnoreCase))
					fc = Prs.Decompress(filename);
				else
					fc = File.ReadAllBytes(filename);
				string path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(filename)), Path.GetFileNameWithoutExtension(filename));
				JsonSerializer js = new JsonSerializer();
				EventExtraIniData ini;
				using (TextReader tr = File.OpenText(Path.Combine(path, Path.ChangeExtension(Path.GetFileName(filename), ".json"))))
				using (JsonTextReader jtr = new JsonTextReader(tr))
					ini = js.Deserialize<EventExtraIniData>(jtr);
				bool battle = ini.Game == Game.SA2B;
				bool dcbeta = ini.Game == Game.SA2;
				bool language = ini.LanguageOnly;
				if (!isBigEndian.HasValue)
					ByteConverter.BigEndian = ini.BigEndian;
				else
					ByteConverter.BigEndian = isBigEndian.Value;
				if (!isLanguageFile.HasValue)
					language = ini.LanguageOnly;
				else
					language = isLanguageFile.Value;
				List<byte> extradata = new List<byte>();
				foreach (SubtitleInfo subs in ini.Subtitles)
				{
					extradata.AddRange(ByteConverter.GetBytes(subs.FrameStart));
					extradata.AddRange(ByteConverter.GetBytes(subs.VisibleTime));
				}
				foreach (AudioInfo audio in ini.AudioInfo)
				{
					extradata.AddRange(audio.GetBytes());
				}
				if (!language)
				{
					foreach (ScreenEffects screen in ini.ScreenEffects)
					{
						if (battle)
							extradata.AddRange(screen.GetBytesGC());
						else
							extradata.AddRange(screen.GetBytesDC());
					}

					foreach (ParticleEffects particle in ini.ParticleEffects)
					{
						extradata.AddRange(particle.GetBytes());
					}

					foreach (LightingInfo light in ini.Lighting)
					{
						extradata.AddRange(light.GetBytes());
					}
					extradata.AddRange(new byte[16]);
					foreach (ParticleEffects2 particle2 in ini.ParticleEffects2)
					{
						extradata.AddRange(particle2.GetBytes());
					}
					foreach (VideoInfo video in ini.VideoInfo)
					{
						extradata.AddRange(video.GetBytes());
					}
				}
				if (Path.GetExtension(filename).Equals(".prs", StringComparison.OrdinalIgnoreCase))
					FraGag.Compression.Prs.Compress(extradata.ToArray(), filename);
				else
					File.WriteAllBytes(filename, extradata.ToArray());
			}
			finally
			{
				Environment.CurrentDirectory = dir;
			}
		}
		public static void BuildMini(bool? isBigEndian, string filename)
		{
			string dir = Environment.CurrentDirectory;
			try
			{
				byte[] fc;
				if (Path.GetExtension(filename).Equals(".prs", StringComparison.OrdinalIgnoreCase))
					fc = Prs.Decompress(filename);
				else
					fc = File.ReadAllBytes(filename);
				string path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(filename)), Path.GetFileNameWithoutExtension(filename));
				JsonSerializer js = new JsonSerializer();
				MiniEventExtraIniData ini;
				using (TextReader tr = File.OpenText(Path.Combine(path, Path.ChangeExtension(Path.GetFileName(filename), ".json"))))
				using (JsonTextReader jtr = new JsonTextReader(tr))
					ini = js.Deserialize<MiniEventExtraIniData>(jtr);
				if (!isBigEndian.HasValue)
					ByteConverter.BigEndian = ini.BigEndian;
				else
					ByteConverter.BigEndian = isBigEndian.Value;
				List<byte> extradata = new List<byte>();
				foreach (SubtitleInfo subs in ini.Subtitles)
				{
					extradata.AddRange(ByteConverter.GetBytes(subs.FrameStart));
					extradata.AddRange(ByteConverter.GetBytes(subs.VisibleTime));
				}
				foreach (EffectInfo effect in ini.Effects)
				{
					extradata.AddRange(effect.GetBytes());
				}
				foreach (MiscMiniEffect misc in ini.Unknown)
				{
					extradata.AddRange(misc.GetBytes());
				}
				if (Path.GetExtension(filename).Equals(".prs", StringComparison.OrdinalIgnoreCase))
					FraGag.Compression.Prs.Compress(extradata.ToArray(), filename);
				else
					File.WriteAllBytes(filename, extradata.ToArray());
			}
			finally
			{
				Environment.CurrentDirectory = dir;
			}
		}
	}
	public class EventExtraIniData
	{
		public string Name { get; set; }
		[JsonIgnore]
		public Game Game { get; set; }
		[JsonProperty(PropertyName = "Game")]
		public string GameString
		{
			get { return Game.ToString(); }
			set { Game = (Game)Enum.Parse(typeof(Game), value); }
		}
		public bool BigEndian { get; set; }
		public bool LanguageOnly { get; set; }
		public List<SubtitleInfo> Subtitles { get; set; } = new List<SubtitleInfo>();
		public List<AudioInfo> AudioInfo { get; set; } = new List<AudioInfo>();
		public List<ScreenEffects> ScreenEffects { get; set; } = new List<ScreenEffects>();
		public List<ParticleEffects> ParticleEffects { get; set; } = new List<ParticleEffects>();
		public List<LightingInfo> Lighting { get; set; } = new List<LightingInfo>();
		public List<ParticleEffects2> ParticleEffects2 { get; set; } = new List<ParticleEffects2>();
		public List<VideoInfo> VideoInfo { get; set; } = new List<VideoInfo>();
	}

	public class SubtitleInfo
	{
		public uint FrameStart { get; set; }
		public uint VisibleTime { get; set; }
	}

	[Serializable]
	public class AudioInfo
	{
		public uint FrameStart { get; set; }
		public string VoiceEntry1 { get; set; }
		public string VoiceEntry2 { get; set; }
		public string MusicEntry { get; set; }

		public static int Size { get { return 0x48; } }

		public byte[] GetBytes()
		{
			List<byte> result = new List<byte>(Size);
			result.AddRange(ByteConverter.GetBytes(FrameStart));
			result.AddRange(ByteConverter.GetBytes((short)Convert.ToUInt16(VoiceEntry1, 16)));
			result.AddRange(ByteConverter.GetBytes((short)Convert.ToUInt16(VoiceEntry2, 16)));
			result.AddRange(Encoding.ASCII.GetBytes(MusicEntry));
			result.Align(0x48);
			return result.ToArray();
		}
	}

	[Serializable]
	public class ScreenEffects
	{
		public uint FrameStart { get; set; }
		public byte Type { get; set; }
		public byte A { get; set; }
		public byte R { get; set; }
		public byte G { get; set; }
		public byte B { get; set; }

		public static int Size { get { return 0x40; } }

		public byte[] GetBytesGC()
		{
			List<byte> result = new List<byte>(Size);
			result.AddRange(ByteConverter.GetBytes(FrameStart));
			result.Add(Type);
			result.AddRange(new byte[3]);
			result.Add(A);
			result.Add(R);
			result.Add(G);
			result.Add(B);
			result.Align(0x40);
			return result.ToArray();
		}

		public byte[] GetBytesDC()
		{
			List<byte> result = new List<byte>(Size);
			result.AddRange(ByteConverter.GetBytes(FrameStart));
			result.Add(Type);
			result.AddRange(new byte[3]);
			result.Add(B);
			result.Add(G);
			result.Add(R);
			result.Add(A);
			result.Align(0x40);
			return result.ToArray();
		}
	}

	[Serializable]
	public class ParticleEffects
	{
		public uint FrameStart { get; set; }
		public byte Type1 { get; set; }
		public byte Type2 { get; set; }
		public Vertex Position { get; set; }
		public float Intensity { get; set; }
		public static int Size { get { return 0x38; } }

		public byte[] GetBytes()
		{
			List<byte> result = new List<byte>(Size);
			result.AddRange(ByteConverter.GetBytes(FrameStart));
			result.Add(Type1);
			result.Add(Type2);
			result.AddRange(new byte[2]);
			result.AddRange(Position.GetBytes());
			result.AddRange(ByteConverter.GetBytes(Intensity));
			result.Align(0x38);
			return result.ToArray();
		}
	}
	[Serializable]
	public class LightingInfo
	{
		public uint FrameStart { get; set; }
		public int FadeType { get; set; }
		public Vertex LightDirection { get; set; }
		public Vertex Color { get; set; }
		public float Intensity { get; set; }
		public Vertex AmbientColor { get; set; }
		public static int Size { get { return 0x44; } }

		public byte[] GetBytes()
		{
			List<byte> result = new List<byte>(Size);
			result.AddRange(ByteConverter.GetBytes(FrameStart));
			result.AddRange(ByteConverter.GetBytes(FadeType));
			result.AddRange(LightDirection.GetBytes());
			result.AddRange(Color.GetBytes());
			result.AddRange(ByteConverter.GetBytes(Intensity));
			result.AddRange(AmbientColor.GetBytes());
			result.Align(0x44);
			return result.ToArray();
		}
	}
	[Serializable]
	public class ParticleEffects2
	{
		public Vertex Position { get; set; }
		public Vertex Unk2 { get; set; }
		public short Unk3 { get; set; }
		public short Unk4 { get; set; }
		public short Unk5 { get; set; }
		public short Unk6 { get; set; }
		public uint FrameStart { get; set; }
		public Vertex Spread { get; set; }
		public int Count { get; set; }
		public int Unk9 { get; set; }
		public int Type { get; set; }
		public int Unk11 { get; set; }

		public static int Size { get { return 0x40; } }

		public byte[] GetBytes()
		{
			List<byte> result = new List<byte>(Size);
			result.AddRange(Position.GetBytes());
			result.AddRange(Unk2.GetBytes());
			result.AddRange(ByteConverter.GetBytes(Unk3));
			result.AddRange(ByteConverter.GetBytes(Unk4));
			result.AddRange(ByteConverter.GetBytes(Unk5));
			result.AddRange(ByteConverter.GetBytes(Unk6));
			result.AddRange(ByteConverter.GetBytes(FrameStart));
			result.AddRange(Spread.GetBytes());
			result.AddRange(ByteConverter.GetBytes(Count));
			result.AddRange(ByteConverter.GetBytes(Unk9));
			result.AddRange(ByteConverter.GetBytes(Type));
			result.AddRange(ByteConverter.GetBytes(Unk11));
			result.Align(0x40);
			return result.ToArray();
		}
	}
	[Serializable]
	public class VideoInfo
	{
		public uint FrameStart { get; set; }
		public ushort VideoType { get; set; }
		public ushort Unknown { get; set; }
		public float Depth { get; set; }
		public byte OverlayType { get; set; }
		public byte OverlayType2 { get; set; }
		public string VideoName { get; set; }
		public static int Size { get { return 0x40; } }

		public byte[] GetBytes()
		{
			List<byte> result = new List<byte>(Size);
			result.AddRange(ByteConverter.GetBytes(FrameStart));
			result.AddRange(ByteConverter.GetBytes(VideoType));
			result.AddRange(ByteConverter.GetBytes(Unknown));
			result.AddRange(ByteConverter.GetBytes(Depth));
			result.Add(OverlayType);
			result.Add(OverlayType2);
			result.AddRange(new byte[2]);
			result.AddRange(Encoding.ASCII.GetBytes(VideoName));
			result.Align(0x40);
			return result.ToArray();
		}
	}
	public class MiniEventExtraIniData
	{
		public string Name { get; set; }
		[JsonIgnore]
		public Game Game { get; set; }
		[JsonProperty(PropertyName = "Game")]
		public string GameString
		{
			get { return Game.ToString(); }
			set { Game = (Game)Enum.Parse(typeof(Game), value); }
		}
		public bool BigEndian { get; set; }
		public List<SubtitleInfo> Subtitles { get; set; } = new List<SubtitleInfo>();
		public List<EffectInfo> Effects { get; set; } = new List<EffectInfo>();
		public List<MiscMiniEffect> Unknown { get; set; } = new List<MiscMiniEffect>();
	}

	[Serializable]
	public class EffectInfo
	{
		public uint FrameStart { get; set; }
		public byte FadeType { get; set; }
		public byte SFXEntry1 { get; set; }
		public byte SFXEntry2 { get; set; }
		public string VoiceEntry { get; set; }
		public string MusicEntry { get; set; }

		public static int Size { get { return 0x4C; } }

		public byte[] GetBytes()
		{
			List<byte> result = new List<byte>(Size);
			result.AddRange(ByteConverter.GetBytes(FrameStart));
			result.Add(FadeType);
			result.Add(SFXEntry1);
			result.Add(SFXEntry2);
			result.AddRange(new byte[1]);
			result.AddRange(ByteConverter.GetBytes((short)Convert.ToUInt16(VoiceEntry, 16)));
			result.AddRange(Encoding.ASCII.GetBytes(MusicEntry));
			result.Align(0x4C);
			return result.ToArray();
		}
	}
	[Serializable]
	public class MiscMiniEffect
	{
		public Vertex Unk1 { get; set; }
		public float Unk2 { get; set; }
		public Vertex Unk3 { get; set; }

		public static int Size { get { return 0x1C; } }

		public byte[] GetBytes()
		{
			List<byte> result = new List<byte>(Size);
			result.AddRange(Unk1.GetBytes());
			result.AddRange(ByteConverter.GetBytes(Unk2));
			result.AddRange(Unk3.GetBytes());
			result.Align(0x1C);
			return result.ToArray();
		}
	}
}
