﻿using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PE.Nominal
{
    /// <summary>
    /// Contains all Actions related to the Nominal Ledger, Including Mappings and Creating Journals
    /// </summary>
    public class NominalDAL
    {
        private readonly DbContext context;

        /// <summary>
        /// Actions Service Expects a DbContext to be injected pointing to the correct database
        /// </summary>
        /// <param name="context"></param>
        public NominalDAL(DbContext context)
        {
            this.context = context;
        }


        public async Task CreateMapCmd()
        {
            await context.Database.ExecuteSqlCommandAsync("pe_NL_Map_Create").ConfigureAwait(false);
        }

        public async Task ClearMapCmd()
        {
            await context.Database.ExecuteSqlCommandAsync("pe_NL_Map_Clear").ConfigureAwait(false);
        }

        public async Task ClearDisbMapCmd()
        {
            await context.Database.ExecuteSqlCommandAsync("pe_NL_DisbMap_Clear").ConfigureAwait(false);
        }

        public async Task<GLSetup> SetupQuery()
        {
            var results = await context.Database.SqlQueryAsync<GLSetup>("pe_NL_Control_Details").ConfigureAwait(false);
            return results.FirstOrDefault();
        }

        public async Task SaveSetupCmd(GLSetup setup)
        {
            await context.Database.ExecuteSqlCommandAsync("pe_NL_Control_Update {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}",
                // Index 0
                setup.WIPServ, setup.WIPPart, setup.WIPOffice, setup.WIPDept, setup.WIPLevel, setup.DRSServ, setup.DRSPart, setup.DRSOffice, setup.DRSDept, setup.DRSLevel, // Ends with Index 9
                // Index 10
                setup.IntSystem, setup.FeeSource, setup.FeeProfit, setup.FeePart, setup.DisbLevel, setup.DisbStd, setup.InterCo, setup.Cashbook, setup.Expenses // Ends with Index 18
                ).ConfigureAwait(false);
        }

        public async Task<IEnumerable<DisbCode>> DisbCodesQuery()
        {
            var results = await context.Database.SqlQueryAsync<DisbCode>("pe_NL_Control_Disbs").ConfigureAwait(false);
            return results;
        }

        public async Task<GLNumEntries> NumEntriesQuery()
        {
            var results = await context.Database.SqlQueryAsync<GLNumEntries>("pe_NL_Num_Entries").ConfigureAwait(false);
            return results.FirstOrDefault();
        }

        public async Task ExtractOpeningCmd()
        {
            var result = new SqlParameter("@Result", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            await context.Database.ExecuteSqlCommandAsync("pe_NL_Opening @Result", result).ConfigureAwait(false);
        }

        public async Task ExtractWIPCmd()
        {
            var result = new SqlParameter("@Result", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            await context.Database.ExecuteSqlCommandAsync("pe_NL_WIP @Result", result).ConfigureAwait(false);

        }

        public async Task ExtractDebtorsCmd()
        {
            var result = new SqlParameter("@Result", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };

            await context.Database.ExecuteSqlCommandAsync("pe_NL_DRS @Result", result).ConfigureAwait(false);

        }

        public async Task ExtractLodgementsCmd()
        {
            var result = new SqlParameter("@Result", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };

            await context.Database.ExecuteSqlCommandAsync("pe_NL_LOD @Result", result).ConfigureAwait(false);

        }

        public async Task<IEnumerable<PostPeriods>> PostPeriodsQuery()
        {
            var results = await context.Database.SqlQueryAsync<PostPeriods>("pe_NL_Post_Periods").ConfigureAwait(false);
            return results;
        }

        public async Task PostCreateCmd(int PeriodIndex)
        {
            await context.Database.ExecuteSqlCommandAsync("pe_NL_Post_Create {0}", PeriodIndex).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MissingMap>> MissingMappingsQuery()
        {
            var results = await context.Database.SqlQueryAsync<MissingMap>("pe_NL_Missing_Map").ConfigureAwait(false);
            return results;
        }

        public async Task<GLMapping> MappingDetailQuery(int MapIndex)
        {
            var results = await context.Database.SqlQueryAsync<GLMapping>("pe_NL_Map_Line {0}", MapIndex).ConfigureAwait(false);
            return results.FirstOrDefault();
        }

        public async Task SaveAccountMappingCmd(int MapIndex, string AccountCode, string AccountTypeCode)
        {
            await context.Database.ExecuteSqlCommandAsync("pe_NL_Map_Line_Update {0}, {1}, {2}", MapIndex, AccountCode, AccountTypeCode).ConfigureAwait(false);
        }

        public async Task<IEnumerable<GLMapping>> NLMappingsQuery()
        {
            var results = await context.Database.SqlQueryAsync<GLMapping>("pe_NL_Mapping_List").ConfigureAwait(false);
            return results;
        }
        public async Task<IEnumerable<JournalGroup>> JournalGroupsQuery() 
        {
            var results = await context.Database.SqlQueryAsync<JournalGroup>("pe_NL_Journal_Groups").ConfigureAwait(false);
            return results;
        }

        public async Task<IEnumerable<JournalMap>> JournalListQuery(int NomOrg, string NomSource, string NomSection, string NomAccount, string NomOffice, string NomService, int? NomPartner, string NomDept)
        {
            var results = await context.Database.SqlQueryAsync<JournalMap>("pe_NL_Journal_Lines {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", NomOrg, NomSource, NomSection, NomAccount, NomOffice, NomService, NomPartner, NomDept).ConfigureAwait(false);
            return results;
        }

        public async Task<IEnumerable<JournalExtract>> ExtractJournalQuery(int BatchID = 0)
        {
            var results = await context.Database.SqlQueryAsync<JournalExtract>("pe_NL_Journal_Export {0}", BatchID).ConfigureAwait(false);
            return results;
        }

        public async Task<IEnumerable<JournalExtract>> TransferJournalQuery(int Org, int BatchID = 0, string Journal = null, string HangfireJobId = null)
        {
            var results = await context.Database.SqlQueryAsync<JournalExtract>("pe_NL_Journal_Transfer {0}, {1}, {2}, {3}", Org, BatchID, Journal, HangfireJobId).ConfigureAwait(false);
            return results;
        }

        public async Task FlagTransferredCmd(int Org, string Journal = null, string HangfireJobId = null)
        {
            await context.Database.ExecuteSqlCommandAsync("pe_NL_Journal_Transfer_Worked {0}, {1}, {2}", Org, Journal, HangfireJobId).ConfigureAwait(false);
        }

        public async Task UnFlagTransferredCmd(int Org, string Journal = null, string HangfireJobId = null)
        {
            await context.Database.ExecuteSqlCommandAsync("pe_NL_Journal_Transfer_Failed {0}, {1}, {2}", Org, Journal, HangfireJobId).ConfigureAwait(false);
        }

        public async Task<IEnumerable<JournalRepostBatch>> JournalRepostListQuery(int NomPeriodIndex)
        {
            var results = await context.Database.SqlQueryAsync<JournalRepostBatch>("pe_NL_Journal_Repost_List {0}", NomPeriodIndex).ConfigureAwait(false);
            return results;
        }

        public async Task<IEnumerable<NomOrganisation>> OrgListQuery()
        {
            var results = await context.Database.SqlQueryAsync<NomOrganisation>("pe_NL_Org_List").ConfigureAwait(false);
            return results;
        }

        public async Task UpdateOrgCmd(int PracID, string NLServer, string NLDatabase, bool NLTransfer)
        {
            await context.Database.ExecuteSqlCommandAsync("pe_NL_Org_Update {0}, {1}, {2}, {3}", PracID, NLServer, NLDatabase, NLTransfer).ConfigureAwait(false);
        }

        public async Task<IEnumerable<JournalExtract>> ExtractBankRecQuery(int Org, int BatchID=0, string Journal = null)
        {
            var results = await context.Database.SqlQueryAsync<JournalExtract>("pe_NL_Cashbook_Extract {0}, {1}, {2}", Org, BatchID, Journal).ConfigureAwait(false);
            return results;
        }

        public async Task FlagBankRecTransferredCmd(int PracID, string journal)
        {
            await context.Database.ExecuteSqlCommandAsync("pe_NL_Cashbook_Worked {0}, {1}", PracID, journal).ConfigureAwait(false);
        }

        public async Task<IEnumerable<PostPeriods>> JournalPeriodsQuery()
        {
            var results = await context.Database.SqlQueryAsync<PostPeriods>("pe_NL_Journal_Periods").ConfigureAwait(false);
            return results;
        }

        public async Task<IEnumerable<CashbookRepostBatch>> BankRecRepostListQuery(int NomPeriodIndex)
        {
            var results = await context.Database.SqlQueryAsync<CashbookRepostBatch>("pe_NL_Cashbook_List {0}", NomPeriodIndex).ConfigureAwait(false);
            return results;
        }
    }
}