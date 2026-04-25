@{
    Rules = @{
        PSAvoidUsingWriteHost = @{
            Enable = $true
            Severity = 'Warning'
        }
        PSAvoidUsingPlainTextForPassword = @{
            Enable = $true
            Severity = 'Error'
        }
        PSAvoidUsingConvertToSecureStringWithPlainText = @{
            Enable = $true
            Severity = 'Error'
        }
        PSUseDeclaredVarsMoreThanAssignments = @{
            Enable = $true
            Severity = 'Warning'
        }
    }

    IncludeRules = @('*')
    ExcludeRules = @(
        'PSReviewUnusedParameter'
    )
}
