﻿/*
CUSTOM: This Stored Procedure should be customized for your environment
This StoredProcedure Creates the Nominal Ledger in bulk from the Lodgement (Bank Deposits) Transactions in PE
*/


CREATE PROCEDURE [dbo].[pe_NL_LOD]

@Result int OUTPUT

AS

DECLARE @MonthEnd datetime
DECLARE @PeriodIdx int

	SET @MonthEnd  = (Select PracPeriodEnd From tblControl Where PracID = 1)
	IF @MonthEnd IS NULL GOTO TRAN_ABORT

	SET @PeriodIdx = (Select PeriodIndex From tblControlPeriods WHERE PeriodEndDate = @MonthEnd)

	BEGIN TRAN

	EXEC pe_NL_Lodge_Update

	IF @@ERROR <> 0 GOTO TRAN_ABORT

	DECLARE @LodInd int
	DECLARE @LodDrs int

	DECLARE csr_Trans CURSOR DYNAMIC 
	FOR SELECT L.LodgeDetIndex, L.LodgeDebtor
	FROM tblTranNominalBank NB INNER JOIN tblLodgementDetails L ON NB.LodgeIndex = L.LodgeIndex
	WHERE NB.LodgePosted = 0

	OPEN csr_Trans

	FETCH csr_Trans INTO @LodInd, @LodDrs
	WHILE (@@FETCH_STATUS=0) 
		BEGIN
		--/Credit Bank Control (B/S)
		INSERT INTO tblTranNominal ( NLPeriodIndex, NLDate, NLOrg, NLSource, NLSection, NLAccount, Office, TransTypeIndex, ContIndex, TransRefAlpha, Amount, NLNarrative, RefMin, RefMax )
		SELECT NLPeriodIndex = CASE WHEN NB.LodgeDate > @MonthEnd THEN 0 ELSE @PeriodIdx END, NB.LodgeDate, NB.LodgePrac, 
			'LOD' AS NLSource, 'BS' AS NLSection, 'BNKCON' AS TranType, TB.BankOffice, '9' AS TransTypeIndex, L.ContIndex, L.LodgeIndex,  
			L.LodgeAmount*-1,  L.LodgePayor, L.LodgeDetIndex,  L.LodgeDetIndex
		FROM tblTranNominalBank NB INNER JOIN tblLodgementDetails L ON NB.LodgeIndex = L.LodgeIndex INNER JOIN tblTranBank TB ON NB.LodgeBank = TB.BankIndex
		WHERE L.LodgeDetIndex = @LodInd
	
		IF @@ERROR <> 0 GOTO TRAN_ABORT

		--/Debit Bank (B/S)
		INSERT INTO tblTranNominal ( NLPeriodIndex, NLDate, NLOrg, NLSource, NLSection, NLAccount, Office, TransTypeIndex, ContIndex, TransRefAlpha, Amount, NLNarrative, RefMin, RefMax )
		SELECT NLPeriodIndex = CASE WHEN NB.LodgeDate > @MonthEnd THEN 0 ELSE @PeriodIdx END, NB.LodgeDate, NB.LodgePrac, 
			'LOD' AS NLSource, 'BS' AS NLSection, 'BANK'+Cast(NB.LodgeBank AS Varchar(2)) AS NLAccount, TB.BankOffice, '9' AS TransTypeIndex, L.ContIndex, L.LodgeIndex,  
			L.LodgeAmount,  L.LodgePayor, L.LodgeDetIndex,  L.LodgeDetIndex
		FROM tblTranNominalBank NB INNER JOIN tblLodgementDetails L ON NB.LodgeIndex = L.LodgeIndex INNER JOIN tblTranBank TB ON NB.LodgeBank = TB.BankIndex
		WHERE L.LodgeDetIndex = @LodInd 
	
		IF @@ERROR <> 0 GOTO TRAN_ABORT

		FETCH 	csr_Trans INTO @LodInd, @LodDrs
		END

	CLOSE csr_Trans
	DEALLOCATE csr_Trans

	UPDATE tblTranNominalBank
	SET LodgePosted = 1
	WHERE LodgePosted = 0

	IF @@ERROR <> 0 GOTO TRAN_ABORT

	COMMIT TRAN
	
	SET @Result = 0
	
	GOTO FINISH

TRAN_ABORT:
	ROLLBACK TRAN
	SET @Result = 1

FINISH: