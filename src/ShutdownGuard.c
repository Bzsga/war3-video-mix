// Classic 1.27a can enter resource shutdown while holding Storm's file lock.
// Its loader and pending-IO workers need that same lock before signaling done.
// Release it only around the two verified shutdown waits, then restore the
// original recursion depth before returning. All other waits pass through.
typedef DWORD (WINAPI *SHUTDOWN_WAIT_MULTI)(DWORD,const HANDLE*,BOOL,DWORD);
typedef DWORD (WINAPI *SHUTDOWN_WAIT_SINGLE)(HANDLE,DWORD);
typedef DWORD (WINAPI *SHUTDOWN_THREAD_ID)(void);
typedef void (WINAPI *SHUTDOWN_CS_OPERATION)(LPVOID);
typedef struct {LPVOID debug;long lockCount,recursion;HANDLE owner,semaphore;DWORD spin;} SHUTDOWN_CS;
static SHUTDOWN_WAIT_MULTI shutdownWait;
static SHUTDOWN_WAIT_SINGLE shutdownSingle;
static SHUTDOWN_THREAD_ID shutdownThread;
static SHUTDOWN_CS_OPERATION shutdownEnter,shutdownLeave;
static BYTE *shutdownGame;
static SHUTDOWN_CS *shutdownLock;
static BOOL shutdownInstalled;

static DWORD WINAPI __attribute__((noinline)) ShutdownWait(DWORD count,const HANDLE *handles,BOOL all,DWORD timeout) {
    DWORD result;long depth=0,i;
    if(count==1&&all&&timeout==0xffffffff&&
       __builtin_return_address(0)==shutdownGame+0x1589fe&&
       __builtin_return_address(1)==shutdownGame+0x16adbe&&
       shutdownLock->owner==(HANDLE)shutdownThread()&&
       shutdownLock->recursion>0&&shutdownLock->recursion<=4) {
        depth=shutdownLock->recursion;
        Mark("SHUTDOWN resource join: release Storm file lock\r\n");
        for(i=0;i<depth;i++)shutdownLeave(shutdownLock);
    }
    result=shutdownWait(count,handles,all,timeout);
    if(depth){for(i=0;i<depth;i++)shutdownEnter(shutdownLock);Mark("SHUTDOWN resource joined; lock restored\r\n");}
    return result;
}
static DWORD WINAPI __attribute__((noinline)) ShutdownSingle(HANDLE handle,DWORD timeout) {
    DWORD result;long depth=0,i;
    if(timeout==0xffffffff&&__builtin_return_address(0)==shutdownGame+0x156fcb&&
       __builtin_return_address(1)==shutdownGame+0x4ad9b&&
       __builtin_return_address(2)==shutdownGame+0x48b56&&
       shutdownLock->owner==(HANDLE)shutdownThread()&&
       shutdownLock->recursion>0&&shutdownLock->recursion<=4) {
        depth=shutdownLock->recursion;
        Mark("SHUTDOWN pending IO: release Storm file lock\r\n");
        for(i=0;i<depth;i++)shutdownLeave(shutdownLock);
    }
    result=shutdownSingle(handle,timeout);
    if(depth){for(i=0;i<depth;i++)shutdownEnter(shutdownLock);Mark("SHUTDOWN pending IO drained; lock restored\r\n");}
    return result;
}
static BOOL ShutdownModule(BYTE *base,DWORD timestamp,DWORD imageSize) {
    DWORD pe;
    if(!base||*(WORD*)base!=0x5a4d)return FALSE;
    pe=*(DWORD*)(base+60);
    if(pe<64||pe>4096||*(DWORD*)(base+pe)!=0x4550||*(WORD*)(base+pe+24)!=0x10b)return FALSE;
    return *(DWORD*)(base+pe+8)==timestamp&&*(DWORD*)(base+pe+80)==imageSize;
}
static void InstallShutdownGuard(void) {
    BYTE *storm=(BYTE*)GetModuleHandleW(L"Storm.dll"),*g=(BYTE*)GetModuleHandleW(L"Game.dll");
    HMODULE kernel=GetModuleHandleW(L"kernel32.dll");DWORD multiSlot,singleSlot,lockAddress,old;
    if(shutdownInstalled||!storm||!g)return;
    // Fail closed on a different engine build. Never guess offsets or patch
    // executable instructions; only this build's two original import slots.
    if(!ShutdownModule(g,0x56bd0e1c,0xcd8000)||!ShutdownModule(storm,0x56bd0e34,0x61000)||
       g[0x156fc5]!=0xff||g[0x156fc6]!=0x15||g[0x156fcb]!=0xc3||
       g[0x48b51]!=0xe8||g[0x4ad99]!=0xff||g[0x4ad9a]!=0x10||
       g[0x1589f8]!=0xff||g[0x1589f9]!=0x15||
       g[0x1589fe]!=0x8b||g[0x1589ff]!=0x4d||g[0x158a00]!=0xfc||
       g[0x16adab]!=0x6a||g[0x16adac]!=0xff||g[0x16adad]!=0x6a||g[0x16adae]!=1||
       storm[0x22297]!=0x68||storm[0x2229c]!=0xff||storm[0x2229d]!=0x15||
       storm[0x222c5]!=0x68||*(DWORD*)(storm+0x22298)!=*(DWORD*)(storm+0x222c6)) {
        Mark("SHUTDOWN guard skipped: unsupported engine build\r\n");return;
    }
    multiSlot=*(DWORD*)(g+0x1589fa);singleSlot=*(DWORD*)(g+0x156fc7);lockAddress=*(DWORD*)(storm+0x22298);
    if(multiSlot<(DWORD)g||multiSlot>(DWORD)g+0xcd8000-4||multiSlot%4||
       singleSlot<(DWORD)g||singleSlot>(DWORD)g+0xcd8000-4||singleSlot%4||
       lockAddress<(DWORD)storm||lockAddress>(DWORD)storm+0x61000-sizeof(SHUTDOWN_CS)||lockAddress%4)return;
    shutdownWait=(SHUTDOWN_WAIT_MULTI)GetProcAddress(kernel,"WaitForMultipleObjects");
    shutdownSingle=(SHUTDOWN_WAIT_SINGLE)GetProcAddress(kernel,"WaitForSingleObject");
    shutdownThread=(SHUTDOWN_THREAD_ID)GetProcAddress(kernel,"GetCurrentThreadId");
    shutdownEnter=(SHUTDOWN_CS_OPERATION)GetProcAddress(kernel,"EnterCriticalSection");
    shutdownLeave=(SHUTDOWN_CS_OPERATION)GetProcAddress(kernel,"LeaveCriticalSection");
    if(!shutdownWait||!shutdownSingle||!shutdownThread||!shutdownEnter||!shutdownLeave||
       *(DWORD*)multiSlot!=(DWORD)shutdownWait||*(DWORD*)singleSlot!=(DWORD)shutdownSingle) {
        Mark("SHUTDOWN guard skipped: wait imports changed\r\n");return;
    }
    shutdownGame=g;shutdownLock=(SHUTDOWN_CS*)lockAddress;
    // Make both pages writable before changing either slot. Existing code
    // remains mapped until termination because the bootstrap module is pinned.
    if(!VirtualProtect((LPVOID)multiSlot,4,4,&old))return;
    {DWORD singleOld;
     if(!VirtualProtect((LPVOID)singleSlot,4,4,&singleOld)){VirtualProtect((LPVOID)multiSlot,4,old,&old);return;}
     *(DWORD*)multiSlot=(DWORD)&ShutdownWait;*(DWORD*)singleSlot=(DWORD)&ShutdownSingle;
     VirtualProtect((LPVOID)singleSlot,4,singleOld,&singleOld);VirtualProtect((LPVOID)multiSlot,4,old,&old);}
    shutdownInstalled=TRUE;Mark("SHUTDOWN guard enabled: classic 1.27a\r\n");
}
