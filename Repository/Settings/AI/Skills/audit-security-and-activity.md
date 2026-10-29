# Audit security & activity

Load this skill to answer questions about **who can access what**, **user
activity**, and **security/audit events** by running the built-in System reports.
Read-only — you never modify users, groups, or report files.

## System reports reference

Run these with `report_execute_get_data`, using the exact path shown.

### Configuration / security
| Path | Purpose |
|---|---|
| `System\100 Configuration - Security Summary` | Users, groups, and access rights |
| `System\110 Configuration - List of Windows Groups and Users` | Windows authentication user listing |

### Audit
| Path | Purpose |
|---|---|
| `System\200 Audit - Search` | Searchable audit log |
| `System\210 Audit - Last Errors` | Most recent server errors |
| `System\220 Audit - Login Failures` | Failed authentication attempts |
| `System\250 Audit - Users Activity` | User activity history |
| `System\260 Audit - AI Chat Activity` | AI chats of the web interface: tokens, cost and tool calls per user, agent and model |
| `System\270 Audit - MCP Activity` | MCP server traffic: calls per day, user, client (address and application) and tool, refusals and errors, inventory of the MCP access tokens (status, expiration, last use) |

### Reports / sources / publication
| Path | Purpose |
|---|---|
| `System\300 Reports - Inventory` | Full list of reports in the repository |
| `System\320 Reports - Server Schedule Definitions` | All configured schedules |
| `System\420 Sources - Documentation` | Datasource schema documentation |
| `System\430 AI - Documentation` | AI agents, skills, tools (with parameters), providers and the security groups using each agent |
| `System\500 Publication - Web Report Server` | Web server publication configuration |
| `System\510 Publication - Repository` | Repository publication details |
| `System\600 System - Repository Size` | Disk usage by folder |

## Security consistency

Call `security_check` (no report needed) when the user asks **why an AI agent or a
tool is refused**, **what he can do**, or to **review the security configuration**:
- scope `mine` (default): effective security of the current user (groups, default
  group, report code level, personal folder, AI agents, web schedules) and the
  warnings of his groups;
- scope `all`: warnings of all the groups (refused if the user cannot execute the
  System reports).

For each warning, explain its consequence in one sentence and the setting to change
in the Server Manager (Configuration > Security), e.g. the repository folder
`\Sources` in 'Read and write' for the Data Source Manager agent, a report folder
with the right Edit, or the report code level 'Metadata and SQL'.

## Workflow

1. For a question on the rights or the refusals of the AI agents, start with
   `security_check`. Otherwise pick the system report that matches the question (use `report_list` if you are
   unsure of the exact path).
2. Run it with `report_execute_get_data`. For audit searches, mention which
   prompted filters the user can adjust at runtime.
3. Summarize the result for the question asked — highlight access rights, recent
   errors, failed logins, or unusual activity, with actionable recommendations.

## Rules
- Read-only — never modify report files or user/group configuration.
- Always use the exact System report path shown above.
- When you mention a report the user could run, include an
  `[EXECUTE_REPORT:System\…|Display Name]` tag on its own line, reproducing the
  path exactly. Emit it verbatim in every language.
