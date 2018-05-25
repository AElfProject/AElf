using System;
using System.Linq;
using System.Collections.Generic;
using AElf.Kernel.Concurrency.Scheduling;
using Xunit;
using Xunit.Sdk;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
	public class BatcherTest
	{
		private ParallelTestDataUtil _dataUtil = new ParallelTestDataUtil();
		private List<Hash> AccountList { get { return _dataUtil.AccountList; } }
		
		public List<ITransaction> GetTestData()
		{
			var txList = _dataUtil.GetFirstGroupTxList();

			return txList;
		}

		public string StringRepresentation(List<ITransaction> l)
		{
			return String.Join(
				" ",
				l.OrderBy(y => AccountList.IndexOf(y.From))
				.ThenBy(z => AccountList.IndexOf(z.To))
				.Select(
					y => String.Format("({0}-{1})", AccountList.IndexOf(y.From), AccountList.IndexOf(y.To))
				));
		}

		[Fact]
		public void TestBatch()
		{
			//var batch = new Batch();
			var txList = GetTestData();

			var batcher = new Batcher();
			var batched = batcher.Process(txList.Select(x => x).ToList());

			var s = batched.Select(
				x => StringRepresentation(x.ToList())
			).OrderBy(b => b).ToList();
			// Test Batch 1
			var expected = StringRepresentation(_dataUtil.GetFirstBatchTxList().Select(x => x).ToList());
			Assert.Equal(expected, s[0]);
		}

		[Fact]
		public void TestJobInBatch()
		{
			var txList = _dataUtil.GetFirstBatchTxList();
			var batcher = new Batcher();
			var grouper = new Grouper();
			var batched = batcher.Process(txList.Select(x => x).ToList());

			var firstBatch = batched.First();
			var jobs = grouper.Process(firstBatch);
			
			Assert.Equal(4, jobs.Count);

			var s = jobs.Select(
				StringRepresentation).ToList();

			for (int jobIndex = 0; jobIndex < 4; jobIndex++)
			{
				Assert.Equal(StringRepresentation(_dataUtil.GetJobTxListInFirstBatch(jobIndex).Select(x => x).ToList()), s[jobIndex]);
			}
		}
	}
}
