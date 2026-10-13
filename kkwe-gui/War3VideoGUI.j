#ifndef WAR3_VIDEO_GUI_INCLUDED
#define WAR3_VIDEO_GUI_INCLUDED
library KKWEVideoSupport initializer Init
globals
    private string session = ""
    private integer sequence = 0
    private real videoVolume = 100.0
    private real videoRate = 1.0
    private string array clips
    private integer array sequences
    private boolean array active
    private real array windowX
    private real array windowY
    private real array windowWidth
    private real array windowHeight
    private real array windowTransparency
endglobals

private function WriteRequest takes integer index, string command returns nothing
    // State is synchronized; only the presentation file write is local.
    // The plugin routes this fixed file to request-<game PID>.pld.
    if GetLocalPlayer() == Player(index) then
        call PreloadGenClear()
        call PreloadGenStart()
        // Separate optional marker keeps old players able to read WV1.
        call Preload("WVR1|" + session + "|" + R2S(windowX[index]) + "|" + R2S(windowY[index]) + "|" + R2S(windowWidth[index]) + "|" + R2S(windowHeight[index]) + "|" + R2S(windowTransparency[index]) + "|END")
        call Preload("WV1|" + session + "|" + I2S(sequences[index]) + "|" + command + "|" + clips[index] + "|0.000|0.000|" + R2S(videoVolume) + "|" + R2S(videoRate) + "|END")
        call PreloadGenEnd("War3Video\\request.pld")
    endif
endfunction

function KKWEVideoStopForPlayer takes player whichPlayer returns nothing
    local integer index
    if whichPlayer == null then
        return
    endif
    set index = GetPlayerId(whichPlayer)
    if index >= 0 and index < 12 and active[index] then
        call WriteRequest(index, "STOP")
        set active[index] = false
    endif
endfunction

function KKWEVideoStopAll takes nothing returns nothing
    local integer index = 0
    loop
        exitwhen index >= 12
        call KKWEVideoStopForPlayer(Player(index))
        set index = index + 1
    endloop
endfunction

function KKWEVideoPlayForPlayer takes player whichPlayer, string videoId returns nothing
    local integer index
    if whichPlayer == null or videoId == "" then
        return
    endif
    set index = GetPlayerId(whichPlayer)
    if index < 0 or index >= 12 then
        return
    endif
    set sequence = sequence + 1
    set sequences[index] = sequence
    set clips[index] = videoId
    set active[index] = true
    call WriteRequest(index, "PLAY")
endfunction

function KKWEVideoPlayAll takes string videoId returns nothing
    local integer index = 0
    if videoId == "" then
        return
    endif
    loop
        exitwhen index >= 12
        call KKWEVideoPlayForPlayer(Player(index), videoId)
        set index = index + 1
    endloop
endfunction

private function UpdateSettings takes nothing returns nothing
    local integer index = 0
    loop
        exitwhen index >= 12
        if active[index] then
            call WriteRequest(index, "PLAY")
        else
            call WriteRequest(index, "SETTINGS")
        endif
        set index = index + 1
    endloop
endfunction

function KKWEVideoSetVolume takes real percent returns nothing
    set videoVolume = RMaxBJ(0.0, RMinBJ(100.0, percent))
    call UpdateSettings()
endfunction

function KKWEVideoSetRate takes real multiplier returns nothing
    set videoRate = RMaxBJ(0.25, RMinBJ(4.0, multiplier))
    call UpdateSettings()
endfunction

private function UpdatePlayerSettings takes integer index returns nothing
    if active[index] then
        call WriteRequest(index, "PLAY")
    else
        call WriteRequest(index, "SETTINGS")
    endif
endfunction

function KKWEVideoSetSizeForPlayer takes player whichPlayer, real widthPercent, real heightPercent returns nothing
    local integer index
    if whichPlayer == null then
        return
    endif
    set index = GetPlayerId(whichPlayer)
    if index < 0 or index >= 12 then
        return
    endif
    set windowWidth[index] = RMaxBJ(1.0, RMinBJ(100.0, widthPercent))
    set windowHeight[index] = RMaxBJ(1.0, RMinBJ(100.0, heightPercent))
    call UpdatePlayerSettings(index)
endfunction

function KKWEVideoSetSizeAll takes real widthPercent, real heightPercent returns nothing
    local integer index = 0
    loop
        exitwhen index >= 12
        call KKWEVideoSetSizeForPlayer(Player(index), widthPercent, heightPercent)
        set index = index + 1
    endloop
endfunction

function KKWEVideoSetPositionForPlayer takes player whichPlayer, real xPercent, real yPercent returns nothing
    local integer index
    if whichPlayer == null then
        return
    endif
    set index = GetPlayerId(whichPlayer)
    if index < 0 or index >= 12 then
        return
    endif
    set windowX[index] = RMaxBJ(0.0, RMinBJ(100.0, xPercent))
    set windowY[index] = RMaxBJ(0.0, RMinBJ(100.0, yPercent))
    call UpdatePlayerSettings(index)
endfunction

function KKWEVideoSetPositionAll takes real xPercent, real yPercent returns nothing
    local integer index = 0
    loop
        exitwhen index >= 12
        call KKWEVideoSetPositionForPlayer(Player(index), xPercent, yPercent)
        set index = index + 1
    endloop
endfunction

function KKWEVideoSetTransparencyForPlayer takes player whichPlayer, real percent returns nothing
    local integer index
    if whichPlayer == null then
        return
    endif
    set index = GetPlayerId(whichPlayer)
    if index < 0 or index >= 12 then
        return
    endif
    set windowTransparency[index] = RMaxBJ(0.0, RMinBJ(100.0, percent))
    call UpdatePlayerSettings(index)
endfunction

function KKWEVideoSetTransparencyAll takes real percent returns nothing
    local integer index = 0
    loop
        exitwhen index >= 12
        call KKWEVideoSetTransparencyForPlayer(Player(index), percent)
        set index = index + 1
    endloop
endfunction

private function Init takes nothing returns nothing
    local integer index = 0
    set session = "kv1_" + I2S(GetRandomInt(100000000, 999999999))
    loop
        exitwhen index >= 12
        set clips[index] = "demo"
        set windowWidth[index] = 100.0
        set windowHeight[index] = 100.0
        set index = index + 1
    endloop
endfunction
endlibrary
#endif
