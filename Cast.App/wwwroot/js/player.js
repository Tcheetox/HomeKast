
// Manage ChromeCast player
// https://github.com/castjs/castjs

import Castjs from './cast.js'

export default class Player {
    #isChrome = /Chrome/.test(navigator.userAgent) && /Google Inc/.test(navigator.vendor)

    #$playerBar = $('.media-player')
    #$headerTitle = $('.header-title')
    #$speaker = $('img.speaker')
    #$castTitle = $('.cast-title')
    #$playPause = $('img.playPause')

    #cjs = null

    constructor() {
        if (this.#$playerBar.length === 0 || !this.#isChrome) return

        this.#cjs = new Castjs();
        if (!this.#cjs) return

        // Bind
        this.#cjs.on('available', () => {
            this.#cjs.on('connect', () => {
                console.log('> Connected')
                this.#onConnected(true)
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
                this.#onConnected(false)
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
        })
    }

    cast = (uri, args) => this.#cjs?.cast(uri, args)
    subtitles = args => this.#cjs?.subtitle(args)

    #updateTitle = title => {
        if (!title || title === '')
            title = 'No title'
        if (title != this.#$castTitle.html())
            this.#$castTitle.html(title)
    }

    #onConnected = (active) => {
        if (active) {
            this.#$playerBar.show()
            //this.#$playerBar.addClass('active')
        } else {
            this.#$playerBar.hide()
            //this.#$playerBar.removeClass('inactive')
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