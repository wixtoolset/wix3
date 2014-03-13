using System;
using System.IO;
using System.Collections;

namespace Microsoft.Deployment.Samples.DDiff
{
	public interface IDiffEngine
	{
		float GetDiffQuality(string diffInput1, string diffInput2, string[] options, IDiffEngineFactory diffFactory);

		bool GetDiff(string diffInput1, string diffInput2, string[] options, TextWriter diffOutput, string linePrefix, IDiffEngineFactory diffFactory);

		IDiffEngine Clone();
	}

	public interface IDiffEngineFactory
	{
		IDiffEngine GetDiffEngine(string diffInput1, string diffInput2, string[] options);
	}

	public class BestQualityDiffEngineFactory : IDiffEngineFactory
	{
		public virtual IDiffEngine GetDiffEngine(string diffInput1, string diffInput2, string[] options)
		{
			float bestDiffQuality = 0;
			IDiffEngine bestDiffEngine = null;

			foreach(IDiffEngine diffEngine in diffEngines)
			{
				float diffQuality = diffEngine.GetDiffQuality(diffInput1, diffInput2, options, this);
				if(diffQuality > bestDiffQuality)
				{
					bestDiffQuality = diffQuality;
					bestDiffEngine = diffEngine;
				}
			}
			return (bestDiffEngine != null ? bestDiffEngine.Clone() : null);
		}

		public BestQualityDiffEngineFactory() : this(null) { }
		public BestQualityDiffEngineFactory(IDiffEngine[] diffEngines)
		{
			this.diffEngines = (diffEngines != null ? new ArrayList(diffEngines) : new ArrayList());
		}

		protected IList diffEngines;

		public virtual void Add(IDiffEngine diffEngine)
		{
			diffEngines.Add(diffEngine);
		}

		public virtual void Remove(IDiffEngine diffEngine)
		{
			diffEngines.Remove(diffEngine);
		}

		public IList DiffEngines
		{
			get
			{
				return ArrayList.ReadOnly(diffEngines);
			}
		}
	}
}
