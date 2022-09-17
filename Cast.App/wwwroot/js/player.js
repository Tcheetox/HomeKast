
// Manage ChromeCast player
// https://github.com/castjs/castjs

// TODO: check for Chrome browser
// TODO: check for events: statechange, event

import Castjs from './cast.js'

export default class Player {
    #$headerBar = $('.info-bar')
    #$playerBar = $('.media-player')
    #$headerTitle = $('.header-title')
    #$speaker = $('img.speaker')
    #$castTitle = $('.cast-title')
    #$playPause = $('img.playPause')

    #cjs = null

    constructor() {
        if (this.#$headerBar.length === 0) return
        this.#cjs = new Castjs();
        if (!this.#cjs) return

        // Bind
        this.#cjs.on('available', () => {
            this.#cjs.on('connect', () => {
                console.log('> Connected')
                this.#onDevice(true)
            })
            this.#cjs.on('playing', () => {
                console.log('> Playing')
                this.#onPlayPause(false)
            })
            this.#cjs.on('pause', () => {
                console.log('> Paused')
                this.#onPlayPause(true)
            })
            this.#cjs.on('disconnect', () => {
                console.log('> Disconnected')
                this.#onDevice(false)
            })
            this.#cjs.on('end', this.#onEnded)
            this.#cjs.on('error', this.#onError)
            this.#cjs.on('mute', () => {
                console.log('> Muted')
                this.#onSpeaker(true)
            })
            this.#cjs.on('unmute', () => {
                console.log('> Unmuted')
                this.#onSpeaker(false)
            })
            this.#cjs.on('statechange', () => {
                console.log('> State change')
                this.#onState()
            })
        })
    }

    cast = (args) => this.#cjs.cast(args)

    #updateTitle = (title = player.title) => {
        if (!title || title === '')
            title = 'No title'
        if (title != $castTitle.html())
            this.#$castTitle.html(title)
    }

    #onState = () => {
        this.#updateTitle()
    }

    #onDevice = (active) => {
        if (active) {
            this.#$playerBar.addClass('active')
            this.#updateTitle()
        } else {
            this.#$playerBar.removeClass('inactive')
        }
    }

    #onPlayPause = (pause) => {
        this.#$playPause.off('click')
        if (pause) {
            this.#$playPause.attr('src', '/media/play.svg')
            this.#$playPause.click(() => player.play())
        } else {
            this.#$playPause.attr('src', '/media/pause.svg')
            this.#$playPause.click(() => player.pause())
        }
    }

    #onEnded = () => {
        console.log('> Ended')
    }

    #onSpeaker = (muted) => {
        this.#$speaker.off('click')
        if (muted) {
            this.#$speaker.attr('src', '/media/muted.svg')
            this.#$speaker.on('click', () => player.unmute())
        } else {
            this.#$speaker.attr('src', '/media/unmuted.svg')
            this.#$speaker.on('click', () => player.mute())
        }
    }

    #onError = (e) => {
        console.log('> Errored')
        console.error(e)
    }
}