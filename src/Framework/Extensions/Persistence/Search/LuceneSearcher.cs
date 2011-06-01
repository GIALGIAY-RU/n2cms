﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using N2.Engine;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using System.Diagnostics;

namespace N2.Persistence.Search
{
	[Service(typeof(ITextSearcher), Replaces = typeof(FindingTextSearcher))]
	public class LuceneSearcher : ITextSearcher
	{
		LuceneAccesor accessor;
		IPersister persister;

		public LuceneSearcher(LuceneAccesor accessor, IPersister persister)
		{
			this.accessor = accessor;
			this.persister = persister;
		}

		#region ITextSearcher Members

		public Result Search(N2.Persistence.Search.Query query)
		{
			var s = accessor.GetSearcher();
			try
			{
				var q = CreateQuery(query);
				var hits = s.Search(q, query.SkipHits + query.TakeHits);

				var result = new Result();
				result.Total = hits.totalHits;
				var resultHits = hits.scoreDocs.Skip(query.SkipHits).Take(query.TakeHits).Select(hit =>
				{
					var doc = s.Doc(hit.doc);
					int id = int.Parse(doc.Get("ID"));
					ContentItem item = persister.Get(id);
					return new Hit { Content = item, Score = hit.score };
				}).ToList();
				result.Hits = resultHits;
				result.Count = resultHits.Count;
				return result;
			}
			finally
			{
				s.Close();
			}
		}

		protected virtual Lucene.Net.Search.Query CreateQuery(N2.Persistence.Search.Query query)
		{
			var q = "";
			if(!string.IsNullOrEmpty(query.Text))
				q = query.OnlyPages.HasValue
					 ? string.Format("+(Title:({0})^4 Text:({0}) PartsText:({0}))", query.Text)
					 : string.Format("+(Title:({0})^4 Text:({0}))", query.Text);

			if (query.Ancestor != null)
				q += string.Format(" +Trail:{0}*", Utility.GetTrail(query.Ancestor));
			if(query.OnlyPages.HasValue)
				q += string.Format(" +IsPage:{0}", query.OnlyPages.Value.ToString().ToLower());
			if (query.Roles != null)
				q += string.Format(" +Roles:(Everyone {0})", string.Join(" ", query.Roles.ToArray()));
			if (query.Types != null)
				q += string.Format(" +Types:({0})", string.Join(" ", query.Types.Select(t => t.Name).ToArray()));
			if (query.LanguageCode != null)
				q += string.Format(" +Language:({0})", query.LanguageCode);
			if (query.Exclution != null)
				q += string.Format(" -({0})", CreateQuery(query.Exclution));
			if (query.Intersection != null)
				q = string.Format("+({0}) +({1})", q, CreateQuery(query.Intersection));
			if (query.Union != null)
				q = string.Format("({0}) ({1})", q, CreateQuery(query.Union));

			Trace.WriteLine("CreateQuery: " + q);

			return accessor.GetQueryParser().Parse(q);
		}
		#endregion
	}
}
