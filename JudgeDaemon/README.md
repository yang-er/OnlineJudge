# Section 1 in judgedaemon.main.php

Enables SSL access without a valid certificate.

# Section 2 in judgedaemon.main.php

Add executive permission to `./build` for its UNIX permission is not correctly set in some cases.

# Section 3 in judgedaemon.main.php

The row `full_judge` is only set in our dom server. When this is set to `false`, standard output and standard error will not be sent. It saves bandwidth and time to process.

# Section 1 in testcase_run.sh

Add memory limit exceeded detection by checking the memory used. This is not accurate, for scenarios that program makes a memory pool and runs into RUNERROR. 

