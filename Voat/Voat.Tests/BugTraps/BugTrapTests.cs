#region LICENSE

/*
    
    Copyright(c) Voat, Inc.

    This file is part of Voat.

    This source file is subject to version 3 of the GPL license,
    that is bundled with this package in the file LICENSE, and is
    available online at http://www.gnu.org/licenses/gpl-3.0.txt;
    you may not use this file except in compliance with the License.

    Software distributed under the License is distributed on an
    "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express
    or implied. See the License for the specific language governing
    rights and limitations under the License.

    All Rights Reserved.

*/

#endregion LICENSE

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voat.Data.Models;
using Voat.Domain.Command;
using Voat.Models;
using System.Threading;
using Voat.Utilities;
using Voat.Common;
using Voat.Tests.Infrastructure;

namespace Voat.Tests.BugTraps
{
    [TestClass]
    public class BugTrapTests
    {
        private int count = 31;
        private int submissionID = 1;

        [TestMethod]
        [TestCategory("Bug"), TestCategory("Voting")]
        public void Bug_Trap_Spam_Votes_VoteCommand()
        {

            int submissionID = 1;
            Submission beforesubmission = GetSubmission();

            int exCount = 0;
            Func<Task<VoteResponse>> vote1 = new Func<Task<VoteResponse>>(async () =>
            {
                var principle = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(USERNAMES.User500CCP, "Bearer"), null);
                System.Threading.Thread.CurrentPrincipal = principle;
                var cmd = new SubmissionVoteCommand(submissionID, 1, IpHash.CreateHash("127.0.0.1"));
                Interlocked.Increment(ref exCount);
                return await cmd.Execute();//.Result;
            });

            Func<Task<VoteResponse>> vote2 = new Func<Task<VoteResponse>>(async () =>
            {
                var principle = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(USERNAMES.User100CCP, "Bearer"), null);
                System.Threading.Thread.CurrentPrincipal = principle;
                var cmd = new SubmissionVoteCommand(submissionID, 1, IpHash.CreateHash("127.0.0.1"));
                Interlocked.Increment(ref exCount);
                return await cmd.Execute();//.Result;
            });

            //exCount = -2;
            //var x = vote1().Result;
            //var y = vote2().Result;


            var tasks = new List<Task<VoteResponse>>();
            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0)
                {
                    tasks.Add(Task.Run(vote1));
                }
                else
                {
                    tasks.Add(Task.Run(vote2));
                }
            }

            Task.WaitAll(tasks.ToArray());

            Submission aftersubmission = GetSubmission();

            Assert.AreEqual(count, exCount, "Execution count is off");
            AssertData(beforesubmission, aftersubmission);
        }
      
        [TestMethod]
        [TestCategory("Bug"), TestCategory("Voting")]
        public void Bug_Trap_Spam_Votes_Repository()
        {
            Submission beforesubmission = GetSubmission();
            int exCount = 0;
            Func<VoteResponse> vote1 = new Func<VoteResponse>(() =>
            {
                var principal = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(USERNAMES.User500CCP, "Bearer"), null);
                System.Threading.Thread.CurrentPrincipal = principal;
                Interlocked.Increment(ref exCount);
                using (var repo = new Voat.Data.Repository(principal))
                {
                    return repo.VoteSubmission(submissionID, 1, IpHash.CreateHash("127.0.0.1"));
                }
            });
            Func<VoteResponse> vote2 = new Func<VoteResponse>(() =>
            {
                var principal = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(USERNAMES.User100CCP, "Bearer"), null);
                System.Threading.Thread.CurrentPrincipal = principal;
                Interlocked.Increment(ref exCount);
                using (var repo = new Voat.Data.Repository(principal))
                {
                    return repo.VoteSubmission(submissionID, 1, IpHash.CreateHash("127.0.0.1"));
                }
            });

            var tasks = new List<Task<VoteResponse>>();
            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0)
                {
                    tasks.Add(Task.Run(vote1));
                }
                else
                {
                    tasks.Add(Task.Run(vote2));
                }
            }

            Task.WaitAll(tasks.ToArray());

            Submission aftersubmission = GetSubmission();
            Assert.AreEqual(count, exCount, "Execution count is off");
            AssertData(beforesubmission, aftersubmission);
        }

        #region Dups

        [TestMethod]
        [TestCategory("Bug")]
        public void Bug_Trap_Spam_Votes_VoteCommand_2()
        {

            int submissionID = 1;
            Submission beforesubmission = GetSubmission();

            int exCount = 0;
            Func<Task<VoteResponse>> vote1 = new Func<Task<VoteResponse>>(async () =>
            {
                var user = TestHelper.SetPrincipal(USERNAMES.User500CCP);
                var cmd = new SubmissionVoteCommand(submissionID, 1, IpHash.CreateHash("127.0.0.1")).SetUserContext(user);
                Interlocked.Increment(ref exCount);
                return await cmd.Execute();//.Result;
            });
            Func<Task<VoteResponse>> vote2 = new Func<Task<VoteResponse>>(async () =>
            {
                var user = TestHelper.SetPrincipal(USERNAMES.User100CCP);
                var cmd = new SubmissionVoteCommand(submissionID, 1, IpHash.CreateHash("127.0.0.1")).SetUserContext(user); ;
                Interlocked.Increment(ref exCount);
                return await cmd.Execute();//.Result;
            });

            var tasks = new List<Task<VoteResponse>>();
            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0)
                {
                    tasks.Add(Task.Run(vote1));
                }
                else
                {
                    tasks.Add(Task.Run(vote2));
                }
            }

            Task.WaitAll(tasks.ToArray());

            Submission aftersubmission = GetSubmission();

            Assert.AreEqual(count, exCount, "Execution count is off");
            AssertData(beforesubmission, aftersubmission);
        }

        [TestMethod]
        [TestCategory("Bug")]
        public void Bug_Trap_Spam_Votes_Repository_2()
        {
            Submission beforesubmission = GetSubmission();
            int exCount = 0;
            Func<VoteResponse> vote1 = new Func<VoteResponse>(() =>
            {
                var user = TestHelper.SetPrincipal(USERNAMES.User500CCP);
                Interlocked.Increment(ref exCount);
                using (var repo = new Voat.Data.Repository(user))
                {
                    return repo.VoteSubmission(submissionID, 1, IpHash.CreateHash("127.0.0.1"));
                }
            });
            Func<VoteResponse> vote2 = new Func<VoteResponse>(() =>
            {
                var user = TestHelper.SetPrincipal(USERNAMES.User100CCP);
                Interlocked.Increment(ref exCount);
                using (var repo = new Voat.Data.Repository(user))
                {
                    return repo.VoteSubmission(submissionID, 1, IpHash.CreateHash("127.0.0.1"));
                }
            });

            var tasks = new List<Task<VoteResponse>>();
            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0)
                {
                    tasks.Add(Task.Run(vote1));
                }
                else
                {
                    tasks.Add(Task.Run(vote2));
                }
            }

            Task.WaitAll(tasks.ToArray());

            Submission aftersubmission = GetSubmission();
            Assert.AreEqual(count, exCount, "Execution count is off");
            AssertData(beforesubmission, aftersubmission);
        }
        #endregion

        private Submission GetSubmission()
        {
            using (var repo = new Voat.Data.Repository())
            {
                return repo.GetSubmission(submissionID);
            }
        }
        private void AssertData(Submission beforeSubmission, Submission afterSubmission)
        {
            long upCountDiff = beforeSubmission.UpCount - afterSubmission.UpCount;
            long downCountDiff = beforeSubmission.DownCount - afterSubmission.DownCount;

            Assert.IsTrue(Math.Abs(upCountDiff + downCountDiff) <= 2, String.Format("Difference detected: UpCount Diff: {0}, Down Count Diff: {1}", upCountDiff, downCountDiff));
            Assert.IsTrue(Math.Abs(upCountDiff) <= 1, String.Format("Before {0} threads: UpCount: {1}, Afterwards: {2}", count, beforeSubmission.UpCount, afterSubmission.UpCount));
            Assert.IsTrue(Math.Abs(downCountDiff) <= 1, String.Format("Before {0} threads: DownCount: {1}, Afterwards: {2}", count, beforeSubmission.DownCount, afterSubmission.DownCount));
        }
    }
}
