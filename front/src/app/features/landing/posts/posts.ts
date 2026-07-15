import { Component } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-posts',
  imports: [RevealDirective],
  templateUrl: './posts.html',
  styleUrl: './posts.css',
})
export class Posts {
  posts = [
    { id: 1, title: 'إعلان عطر رواج الذكي', image: '/post.png' },
    { id: 2, title: 'تصميم بوست ترويجي متميز لعطورك', image: '/post.png' },
    { id: 3, title: 'حملة تسويقية إبداعية مخصصة للتفاعل', image: '/post.png' },
    { id: 4, title: 'منشورات رقمية احترافية لشبكات التواصل', image: '/post.png' },
    { id: 5, title: 'هوية بصرية استثنائية لصورة علامتك التجارية', image: '/post.png' },
    { id: 6, title: 'أفكار إعلانية مبتكرة وجاذبة للجمهور', image: '/post.png' }
  ];

  activeIndex = 2; // Centered element initial value

  next() {
    if (this.activeIndex < this.posts.length - 1) {
      this.activeIndex++;
    } else {
      this.activeIndex = 0;
    }
  }

  prev() {
    if (this.activeIndex > 0) {
      this.activeIndex--;
    } else {
      this.activeIndex = this.posts.length - 1;
    }
  }
}
